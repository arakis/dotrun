using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime.Factories;

public class StepExecutorFactory : IStepExecutorFactory
{
    private readonly INodeFactory _nodeFactory;
    private readonly IShellFactory _shellFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<StepExecutorFactory> _logger;

    public StepExecutorFactory(
        INodeFactory nodeFactory,
        IShellFactory shellFactory,
        ILoggerFactory loggerFactory,
        ILogger<StepExecutorFactory> logger)
    {
        _nodeFactory = nodeFactory ?? throw new ArgumentNullException(nameof(nodeFactory));
        _shellFactory = shellFactory ?? throw new ArgumentNullException(nameof(shellFactory));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        // Get or create the node for this step
        var node = GetOrCreateNode(step, context);

        // Create the shell for this step
        var shell = _shellFactory.CreateShell(step.Shell);

        // Create the step executor with all dependencies
        var executor = new StepExecutor(step, context, node, shell, logger ?? new ConsoleOutput());

        _logger.LogInformation("Created step executor for step: {StepName}", step.Name);
        return executor;
    }

    private INode GetOrCreateNode(Step step, StepContext context)
    {
        // Get the node from WorkflowContext (which caches nodes)
        // This preserves the existing caching behavior
        return context.WorkflowContext.GetNode(step.Node);
    }
}