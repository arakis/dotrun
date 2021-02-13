// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime
{
    public class CmdShell : IShell
    {

        public async Task<StepResult> Execute(StepContext context, ILogger output)
        {
            var command = context.Step.Run;
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(command));
            var tmpScriptPath = "/tmp/script.bat";
            await context.Node.WriteFile(context, tmpScriptPath, ms);
            var result = await context.Node.ExecuteCommand(new NodeCommand
            {
                Context = context.WorkflowContext,
                FileName = "cmd.exe",
                Arguments = new string[] { "/c", tmpScriptPath },
                Output = output,
                Timeout = TimeSpan.MaxValue,
            }).CompletedTask;

            return new StepResult(context)
            {
                Failed = !result.Completed || (result.ExitCode != null && result.ExitCode != 0),
                ExitCode = result.ExitCode,
            };
        }

    }

}
