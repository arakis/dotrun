using System;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime.Factories;

public class StepExecutorFactory : IStepExecutorFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<StepExecutorFactory> _logger;

    public StepExecutorFactory(
        ILoggerFactory loggerFactory,
        ILogger<StepExecutorFactory> logger)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public IStepExecutor CreateStepExecutor(Step step, StepContext context)
        => CreateStepExecutor(step, context, _loggerFactory.CreateLogger<StepExecutor>());

    public IStepExecutor CreateStepExecutor(Step step, StepContext context, ILogger logger)
    {
        if (step == null)
        {
            _logger.LogError("Step is null");
            throw new ArgumentNullException(nameof(step));
        }

        if (context == null)
        {
            _logger.LogError("StepContext is null");
            throw new ArgumentNullException(nameof(context));
        }

        _logger.LogDebug("Creating step executor for step: {StepName}", step.Name);

        // Use the existing factory method from StepExecutor
        var executor = StepExecutor.Create(step, context);

        _logger.LogInformation("Created step executor for step: {StepName}", step.Name);
        return executor;
    }
}