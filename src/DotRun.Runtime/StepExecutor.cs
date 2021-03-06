﻿// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime
{
    public class StepExecutor : IStepExecutor
    {

        public Step Step { get; private set; }
        public StepContext Context { get; private set; }

        public INode Node { get; private set; }

        public IShell Shell { get; private set; }
        public ILogger Output { get; private set; }

        public static IStepExecutor Create(Step step, StepContext context)
        {
            var executor = new StepExecutor(step, context);
            executor.Node = context.WorkflowContext.GetNode(step.Node);
            executor.Shell = context.Node.CreateShell(step.Shell);
            executor.Output = new ConsoleOutput();
            return executor;
        }

        private StepExecutor(Step step, StepContext context)
        {
            Step = step;
            Context = context;
        }

        public Task<StepResult> Run()
        {
            return RunInternal();
        }

        internal async Task<StepResult> RunInternal()
        {
            Output.LogInformation("");
            Output.LogInformation($"*** Start Step {Step.Name} ***");

            if (!string.IsNullOrEmpty(Step.Uses))
                await RunUses();

            if (!string.IsNullOrEmpty(Step.Run))
            {
                Output.LogInformation("Shell: " + Step.Run);
                var result = await Shell.Execute(Context, Output);
                if (result.ExitCode != null)
                    Output.LogInformation("ExitCode: " + result.ExitCode);
                return result;
            }

            return new StepResult(Context);
        }

        private async Task RunUses()
        {
            var use = UseAction.Create(Context);
            if (use != null)
            {
                Output.LogInformation("Step.Uses start: " + Step.Uses);
                await use.Run();
                Output.LogInformation("Step.Uses done: " + Step.Uses);
            }

            return;
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }
    }

}
