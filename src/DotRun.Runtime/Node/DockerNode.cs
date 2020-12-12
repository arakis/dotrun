using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;

namespace DotRun.Runtime
{
    public class DockerNode : Node
    {

        public DockerNode(WorkflowContext context, NodeModel model)
            : base(context, model)
        {
            if (!string.IsNullOrEmpty(Model.Image))
                ImageName = Model.Image;
        }

        public override IShell CreateShell(string name)
        {
            if (string.IsNullOrEmpty(name))
                name = "sh";

            return base.CreateShell(name);
        }

        private static async Task<string> WriteStreamToRandomFile(Stream source)
        {
            var randomPath = Path.GetRandomFileName();

            if (File.Exists(randomPath))
                File.Delete(randomPath);

            using var fileStream = File.Create(randomPath);
            source.Seek(0, SeekOrigin.Begin);
            await source.CopyToAsync(fileStream);

            return randomPath;
        }

        public override async Task WriteFile(StepContext context, string path, Stream source)
        {
            //Client.Containers.ExtractArchiveToContainerAsync(ContainerID, new ContainerPathStatParameters { Path = path }, ...);

            var tempPath = await WriteStreamToRandomFile(source);

            if (path.StartsWith("~"))
                path = (await GetHomeDir()) + path.Substring(1);

            InternalOutput.WriteLine("Writing file " + path);

            await ExecuteLocalCommand(new NodeCommand
            {
                FileName = "docker",
                Arguments = new string[] { "cp", tempPath, ContainerName + ":" + path },
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
                }, null, new Progress());
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
            //ConnectResult.CancellationTokenSource.Cancel();
            //ConnectResult.CompletedTask.Wait();
            //ExecuteLocalCommand(new NodeCommand
            //{
            //    FileName = "docker",
            //    Arguments = new string[] { "rm", "-f", ContainerName },
            //    Output = InternalOutput,
            //}).CompletedTask.Wait();
        }

        public override async Task<string> FindExecutablePath(string executable)
        {
            var output = new MemoryOutput();

            await ExecuteCommand(new NodeCommand
            {
                FileName = "/bin/which",
                Arguments = new string[] { "git" },
                Output = output,
            }).CompletedTask;

            return output.Lines.FirstOrDefault();
        }

        public override async Task<string> GetHomeDir()
        {
            var output = new MemoryOutput();

            await ExecuteCommand(new NodeCommand
            {
                FileName = "/bin/sh",
                Arguments = new string[] { "-c", "'cd ~ && pwd'" },
                Output = output,
            }).CompletedTask;

            return output.Lines.FirstOrDefault();
        }

    }

}
