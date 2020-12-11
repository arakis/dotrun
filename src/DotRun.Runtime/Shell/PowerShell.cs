using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DotRun.Runtime
{
    public class PowerShell : IShell
    {

        public async Task<StepResult> Execute(StepContext context, IOutput output)
        {
            var command = context.Step.Run;
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(command));
            var tmpScriptPath = "/tmp/script.ps1";
            await context.Node.WriteFile(context, tmpScriptPath, ms);
            await context.Node.ExecuteCommand(new ShellCommand
            {
                context = context,
                proc = "pwsh.exe",
                args = new string[] { tmpScriptPath },
                output = output,
                timeout = TimeSpan.MaxValue,
            });
            return new StepResult();
        }

    }

}
