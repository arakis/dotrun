// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime
{
    public class PowerShell : IShell
    {

        private async Task<string> DetectPowerShell(StepContext context)
        {
            var pwsh = await context.Node.FindExecutablePath("pwsh");
            if (pwsh.IsSet())
                return "pwsh.exe";
            else
                return "powershell.exe";
        }

        public async Task<StepResult> Execute(StepContext context, ILogger output)
        {
            var command = context.Step.Run;
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(command));
            var tmpScriptPath = "/tmp/script-" + StringHelper.RandomString() + ".ps1";
            await context.Node.WriteFile(context, tmpScriptPath, ms);
            await context.Node.ExecuteCommand(new NodeCommand
            {
                Context = context.WorkflowContext,
                FileName = await DetectPowerShell(context),
                Arguments = new string[] { tmpScriptPath },
                Output = output,
                Timeout = TimeSpan.MaxValue,
            }).CompletedTask;
            await context.Node.Delete(context, tmpScriptPath);
            return new StepResult();
        }

    }

}
