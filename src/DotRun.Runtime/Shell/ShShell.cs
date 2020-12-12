using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DotRun.Runtime
{
    public class ShShell : IShell
    {

        public async Task<StepResult> Execute(StepContext context, IOutput output)
        {
            var command = context.Step.Run;
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(command));
            var tmpScriptPath = "/tmp/script.sh";
            await context.Node.WriteFile(context, tmpScriptPath, ms);
            await context.Node.ExecuteCommand(new NodeCommand
            {
                Context = context.WorkflowContext,
                FileName = "/bin/sh",
                Arguments = new string[] { tmpScriptPath },
                Output = output,
                Timeout = TimeSpan.MaxValue,
            }).CompletedTask;
            return new StepResult();
        }

    }

}
