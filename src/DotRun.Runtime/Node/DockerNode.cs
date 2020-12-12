using System.IO;
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

        public override async Task WriteFile(StepContext context, string path, Stream source)
        {
            if (File.Exists(path))
                File.Delete(path);

            using var fileStream = File.Create(path);
            source.Seek(0, SeekOrigin.Begin);
            await source.CopyToAsync(fileStream);
        }

        public override RunningProcess ExecuteCommand(NodeCommand cmd)
        {
            return ExecuteLocalCommand(cmd);
        }

        private RunningProcess ConnectResult;
        public override Task<bool> Connect()
        {
            var result = ExecuteLocalCommand(new NodeCommand
            {
                FileName = "docker",
                Arguments = new string[] { "run", "-i", "busybox", "/bin/sh" },
                Output = InternalOutput,
            });

            //var task = await Task.WhenAny(result.CompletedTask, result.StartedOutput);
            //if (task == result.CompletedTask)
            //    return result.CompletedTask.Result.Completed;

            return result.StartedTask;
        }

        public override void Dispose()
        {
            ConnectResult.CancellationTokenSource.Cancel();
            ConnectResult.CompletedTask.Wait();
        }

    }

}
