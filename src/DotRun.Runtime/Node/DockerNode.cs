using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
                Arguments = new string[] {"exec", "-i", ContainerName, "/bin/sh", "-c",$"{cmd.FileName} -c {string.Join(" ", cmd.Arguments)}"
                },
                Output = InternalOutput,
            });
        }

        private string ContainerName;

        private RunningProcess ConnectResult;
        public override async Task<bool> Connect()
        {
            ContainerName = "dotrun-" + StringHelper.RandomString();
            ConnectResult = ExecuteLocalCommand(new NodeCommand
            {
                FileName = "docker",
                Arguments = new string[] { "run", "-i", "--name", ContainerName, "busybox", "/bin/sh", "-c", "echo started; sleep infinity" },
                Output = InternalOutput,
            });

            var task = await Task.WhenAny(ConnectResult.CompletedTask, ConnectResult.StartedOutput);
            if (task == ConnectResult.CompletedTask)
                return ConnectResult.CompletedTask.Result.Completed;

            InternalOutput.WriteLine("Created Container " + ContainerName);

            return true;
        }

        public override void Dispose()
        {
            ConnectResult.CancellationTokenSource.Cancel();
            ConnectResult.CompletedTask.Wait();
            ExecuteLocalCommand(new NodeCommand
            {
                FileName = "docker",
                Arguments = new string[] { "rm", "-f", ContainerName },
                Output = InternalOutput,
            }).CompletedTask.Wait();
        }

    }

}
