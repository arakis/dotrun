// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public override RunningProcess ExecuteCommand(NodeCommand cmd)
        {
            using var c = SshClient.CreateCommand($"{cmd.FileName} {string.Join(" ", cmd.Arguments)}");
            c.Execute();

            var procResult = new ProcessResult
            {
                ExitCode = c.ExitStatus,
                Completed = true,
            };

            if (c.Result.IsSet())
                cmd.Output.WriteLine(c.Result);

            if (c.Error.IsSet())
                cmd.Output.ErrorLine(c.Error);

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
