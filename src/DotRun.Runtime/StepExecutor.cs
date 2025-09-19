// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DotRun.Runtime.Factories;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime;

public class StepExecutor : IStepExecutor
{
    private readonly INodeFactory _nodeFactory;
    private readonly IShellFactory _shellFactory;
    private readonly ILogger _logger;

    public Step Step { get; private set; }
    public StepContext Context { get; private set; }
    public INode Node { get; private set; }
    public IShell Shell { get; private set; }
    public ILogger Output { get; private set; }

    public StepExecutor(
        Step step,
        StepContext context,
        INode node,
        IShell shell,
        ILogger logger)
    {
        Step = step ?? throw new ArgumentNullException(nameof(step));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Node = node ?? throw new ArgumentNullException(nameof(node));
        Shell = shell ?? throw new ArgumentNullException(nameof(shell));
        Output = logger ?? throw new ArgumentNullException(nameof(logger));
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