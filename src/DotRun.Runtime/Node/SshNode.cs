// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace DotRun.Runtime
{
    public class SshNode : Node
    {

        public SshNode(WorkflowContext context, NodeModel model)
            : base(context, model)
        {
            Host = Model.Host;
            Username = Model.Username;
            Username = context.GetNode("local").GetUserName().Result;
            if (!string.IsNullOrEmpty(Model.KeyFile))
                KeyFile = Model.KeyFile;

            Platform = new WindowsPlatform(this);
        }

        public override IShell CreateShell(string name)
        {
            if (string.IsNullOrEmpty(name))
                name = "ps";

            return base.CreateShell(name);
        }

        public override Task WriteFile(StepContext context, string path, Stream source)
        {
            return Task.Run(() => ScpClient.Upload(source, path));
        }

        private class ReceivedHandler : IDisposable
        {
            private readonly AutoResetEvent _signal;
            private readonly StringBuilder _buffer = new StringBuilder();
            public ReceivedHandler()
            {
                _signal = new AutoResetEvent(false);
            }

            public void OnReceive(object sender, EventArgs e)
            {
                var dataProp = e.GetType().GetProperty("Data", BindingFlags.Instance | BindingFlags.Public);
                var rawData = (byte[])dataProp.GetValue(e);
                var data = Encoding.ASCII.GetString(rawData);
                lock (_buffer)
                {
                    // append to buffer for reader to consume
                    _buffer.Append(data);
                }

                // notify reader
                try
                {
                    Signal.Set();
                }
                catch // may get a WaitHandle closed exception
                {
                }
            }

            public AutoResetEvent Signal => _signal;

            public string ReadLine()
            {
                lock (_buffer)
                {
                    // cleanup buffer
                    var result = _buffer.ToString();
                    _buffer.Clear();
                    return result;
                }
            }

            public void Dispose()
            {
                _signal.Dispose();
            }
        }

        private class ExtendedReceivedHandler : IDisposable
        {
            private readonly AutoResetEvent _signal;
            private readonly StringBuilder _buffer = new StringBuilder();
            public ExtendedReceivedHandler()
            {
                _signal = new AutoResetEvent(false);
            }

            public void OnReceive(object sender, EventArgs e)
            {
                var dataProp = e.GetType().GetProperty("Data", BindingFlags.Instance | BindingFlags.Public);
                var rawData = (byte[])dataProp.GetValue(e);

                var dataProp2 = e.GetType().GetProperty("DataTypeCode", BindingFlags.Instance | BindingFlags.Public);
                var rawDataTypeCode = (uint)dataProp2.GetValue(e);

                if (rawDataTypeCode == 1)
                {
                    var data = Encoding.ASCII.GetString(rawData);
                    lock (_buffer)
                    {
                        // append to buffer for reader to consume
                        _buffer.Append(data);
                    }
                }

                // notify reader
                try
                {
                    Signal.Set();
                }
                catch // may get a WaitHandle closed exception
                {
                }
            }

            public AutoResetEvent Signal => _signal;

            public string ReadLine()
            {
                lock (_buffer)
                {
                    // cleanup buffer
                    var result = _buffer.ToString();
                    _buffer.Clear();
                    return result;
                }
            }

            public void Dispose()
            {
                _signal.Dispose();
            }
        }

        public override RunningProcess ExecuteCommand(NodeCommand cmd)
        {

            using var command = SshClient.CreateCommand($"{cmd.FileName} {string.Join(" ", cmd.Arguments)}");

            // hacky: https://stackoverflow.com/questions/37059305/c-sharp-streamreader-readline-returning-null-before-end-of-stream
            var result = command.BeginExecute();
            var channelField = command.GetType().GetField("_channel", BindingFlags.Instance | BindingFlags.NonPublic);
            var channel = channelField.GetValue(command);
            var receivedEvent = channel.GetType().GetEvent("DataReceived", BindingFlags.Instance | BindingFlags.Public);
            var extendedDataEvent = channel.GetType().GetEvent("ExtendedDataReceived", BindingFlags.Instance | BindingFlags.Public);

            using var handler = new ReceivedHandler();
            using var handler2 = new ExtendedReceivedHandler();
            // add event handler here
            receivedEvent.AddEventHandler(channel, Delegate.CreateDelegate(receivedEvent.EventHandlerType, handler, handler.GetType().GetMethod("OnReceive")));
            extendedDataEvent.AddEventHandler(channel, Delegate.CreateDelegate(extendedDataEvent.EventHandlerType, handler2, handler2.GetType().GetMethod("OnReceive")));

            while (true)
            {
                // wait on both command completion and our custom wait handle. This is blocking call
                var t = WaitHandle.WaitAny(new[] { result.AsyncWaitHandle, handler.Signal, handler2.Signal });
                // if done - break
                if (result.IsCompleted)
                    break;

                if (t == 1)
                {
                    var line = handler.ReadLine();
                    cmd.Output.WriteLine(line);
                }
                else
                {
                    var line = handler2.ReadLine();
                    cmd.Output.ErrorLine(line);
                }
            }

            var procResult = new ProcessResult
            {
                ExitCode = command.ExitStatus,
                Completed = true,
            };

            return new RunningProcess
            {
                StartedTask = Task.FromResult(true),
                CompletedTask = Task.FromResult(procResult),
                StartedOutput = Task.CompletedTask,
            };
        }

        public string Host { get; internal set; }
        public string Username { get; internal set; }
        public string KeyFile { get; internal set; } = "~/.ssh/id_rsa";

        private SshClient SshClient;
        private ScpClient ScpClient;
        public override async Task<bool> Init()
        {
            var sshTask = Task.Run(() =>
            {
                SshClient = new SshClient(Host, Username, new PrivateKeyFile(DirectoryHelper.GetAbsoluteLocalPath(KeyFile)));
                SshClient.Connect();
                return true;
            });

            var scpTask = Task.Run(() =>
            {
                ScpClient = new ScpClient(Host, Username, new PrivateKeyFile(DirectoryHelper.GetAbsoluteLocalPath(KeyFile)));
                ScpClient.Connect();
                return true;
            });

            await Task.WhenAll(sshTask, scpTask);

            return true;
        }

        public override void Dispose()
        {
            SshClient?.Dispose();
            SshClient = null;
        }

    }

}
