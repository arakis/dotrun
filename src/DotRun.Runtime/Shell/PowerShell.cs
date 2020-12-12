// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            await context.Node.ExecuteCommand(new NodeCommand
            {
                Context = context.WorkflowContext,
                FileName = "pwsh.exe",
                Arguments = new string[] { tmpScriptPath },
                Output = output,
                Timeout = TimeSpan.MaxValue,
            }).CompletedTask;
            return new StepResult();
        }

    }

}
