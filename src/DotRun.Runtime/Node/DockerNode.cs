using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Docker.DotNet;
using Docker.DotNet.Models;
using System;

namespace DotRun.Runtime
{
    public class DockerNode : Node
    {

        public DockerNode(WorkflowContext context)
            : base(context)
        {
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
            var tempPath = await WriteStreamToRandomFile(source);

            await ExecuteLocalCommand(new NodeCommand
            {
                FileName = "docker",
                Arguments = new string[] { "cp", tempPath, ContainerName + ":" + path },
                Output = InternalOutput,
            }).CompletedTask;
        }

        public override RunningProcess ExecuteCommand(NodeCommand cmd)
        {
            return ExecuteLocalCommand(new NodeCommand
            {
                FileName = "docker",
                Arguments = new string[] {"exec", "-i", ContainerName, "/bin/sh", "-c", $"{cmd.FileName} -c {string.Join(" ", cmd.Arguments)}"
                },
                Output = InternalOutput,
            });
        }

        private string ContainerName;
        private int MaxConnectTime = 60 * 60 * 24;
        public string ImageName { get; private set; } = "busybox:latest";

        private class Progress : IProgress<JSONMessage>
        {
            public void Report(JSONMessage value)
            {
                Console.WriteLine(value.Status);
            }
        }

        private DockerClient Client;
        public override async Task<bool> Connect()
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
                //Shell = new string[] { "/bin/sh" },
                Cmd = new string[] { "/bin/sh", "-c", $"echo started; sleep {MaxConnectTime}" },
            });

            await Client.Containers.StartContainerAsync(res.ID, new ContainerStartParameters());

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

    }

}
