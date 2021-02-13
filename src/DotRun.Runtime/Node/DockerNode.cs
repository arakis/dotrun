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

namespace DotRun.Runtime
{
    public class DockerNode : Node
    {

        public DockerNode(WorkflowContext context, NodeModel model)
            : base(context, model)
        {
            if (!string.IsNullOrEmpty(Model.Image))
                ImageName = Model.Image;

            Platform = new UnixPlatform(this);
        }

        public override IShell CreateShell(string name)
        {
            if (string.IsNullOrEmpty(name))
                name = "sh";

            return base.CreateShell(name);
        }

        public override async Task WriteFile(StepContext context, string path, Stream source)
        {
            //Client.Containers.ExtractArchiveToContainerAsync(ContainerID, new ContainerPathStatParameters { Path = path }, ...);

            using var tempFile = await DirectoryHelper.WriteStreamToTempFile(source);

            if (path.StartsWith("~"))
                path = (await GetHomeDir()) + path.Substring(1);

            InternalOutput.LogInformation("Writing file " + path);

            await ExecuteLocalCommand(new NodeCommand
            {
                FileName = "docker",
                Arguments = new string[] { "cp", tempFile.FilePath, ContainerName + ":" + path },
                Output = InternalOutput,
            }).CompletedTask;
        }

        public override RunningProcess ExecuteCommand(NodeCommand cmd)
        {

            var args = new List<string>();
            args.Add("exec");
            args.Add("-i");

            foreach (var env in cmd.Env)
            {
                args.Add("-e");
                args.Add(env.Key + "=" + env.Value);
            }

            args.Add(ContainerName);
            args.Add("/bin/sh");
            args.Add("-c");
            args.Add($"{cmd.FileName} {string.Join(" ", cmd.Arguments)}");

            return ExecuteLocalCommand(new NodeCommand
            {
                FileName = "docker",
                Arguments = args,
                Output = cmd.Output ?? InternalOutput,
            });
        }

        private string ContainerName;
        private string ContainerID;
        private int MaxConnectTime = 60 * 60 * 24;
        public string ImageName { get; internal set; } = "busybox:latest";

        private class Progress : IProgress<JSONMessage>
        {
            public void Report(JSONMessage value)
            {
                Console.WriteLine(value.Status);
            }
        }

        private DockerClient Client;
        public override async Task<bool> Init()
        {
            Client = new DockerClientConfiguration().CreateClient();

            ContainerName = "dotrun-" + StringHelper.RandomString();

            var r = await Client.Images.ListImagesAsync(new ImagesListParameters { MatchName = ImageName });
            if (r.Count == 0)
            {
                await Client.Images.CreateImageAsync(new ImagesCreateParameters
                {
                    FromImage = ImageName,
                },
                null,
                new Progress());
            }

            var res = await Client.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = ImageName,
                Name = ContainerName,
                Cmd = new string[] { "/bin/sh", "-c", $"echo started; sleep {MaxConnectTime}" },
            });
            ContainerID = res.ID;

            await Client.Containers.StartContainerAsync(ContainerID, new ContainerStartParameters());

            return true;
        }

        public override void Dispose()
        {
            Client?.Dispose();
            Client = null;
            base.Dispose();
        }

        public override Task Delete(StepContext context, string path)
        {
            return Platform.Delete(path);
        }

    }

}
