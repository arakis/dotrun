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
            protected IOutput Output;
            public ReceivedHandler(IOutput output)
            {
                Output = output;
            }

            public void OnReceive(object sender, EventArgs e)
            {
                var dataProp = e.GetType().GetProperty("Data", BindingFlags.Instance | BindingFlags.Public);
                var rawData = (byte[])dataProp.GetValue(e);
                var data = Encoding.ASCII.GetString(rawData);
                Output.WriteLine(data);
            }

            public void Dispose()
            {
            }

        }

        private class ExtendedReceivedHandler : IDisposable
        {
            protected IOutput Output;
            public ExtendedReceivedHandler(IOutput output)
            {
                Output = output;
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
                    Output.ErrorLine(data);
                }
            }
            public void Dispose()
            {
            }


        }

        public override RunningProcess ExecuteCommand(NodeCommand cmd)
        {
            var cmdLine = $"{cmd.FileName} {string.Join(" ", cmd.Arguments)}";
            Console.WriteLine("SSH: " + cmdLine);
            using var command = SshClient.CreateCommand(cmdLine);

            // hacky: https://stackoverflow.com/questions/37059305/c-sharp-streamreader-readline-returning-null-before-end-of-stream
            var result = command.BeginExecute();
            var channelField = command.GetType().GetField("_channel", BindingFlags.Instance | BindingFlags.NonPublic);
            var channel = channelField.GetValue(command);
            var receivedEvent = channel.GetType().GetEvent("DataReceived", BindingFlags.Instance | BindingFlags.Public);
            var extendedDataEvent = channel.GetType().GetEvent("ExtendedDataReceived", BindingFlags.Instance | BindingFlags.Public);

            using var handler = new ReceivedHandler(cmd.Output);
            using var handler2 = new ExtendedReceivedHandler(cmd.Output);

            // add event handler here
            receivedEvent.AddEventHandler(channel, Delegate.CreateDelegate(receivedEvent.EventHandlerType, handler, handler.GetType().GetMethod("OnReceive")));
            extendedDataEvent.AddEventHandler(channel, Delegate.CreateDelegate(extendedDataEvent.EventHandlerType, handler2, handler2.GetType().GetMethod("OnReceive")));

            result.AsyncWaitHandle.WaitOne();

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

        public override Task Delete(StepContext context, string path)
        {
            return Platform.Delete(path);
        }
    }

}
