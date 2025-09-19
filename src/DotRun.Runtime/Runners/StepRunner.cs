using System.Threading.Tasks;
using DotRun.Runtime.Factories;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime.Runners;

public class StepRunner : IStepRunner
{
    private readonly IStepExecutorFactory _stepExecutorFactory;
    private readonly ILogger<StepRunner> _logger;

    public StepRunner(
        IStepExecutorFactory stepExecutorFactory,
        ILogger<StepRunner> logger)
    {
        _stepExecutorFactory = stepExecutorFactory;
        _logger = logger;
    }

    public async Task<StepResult> RunAsync(Step step, StepContext stepContext)
    {
        _logger.LogDebug("Creating executor for step: {StepName}", step.Name);
        
        var executor = _stepExecutorFactory.CreateStepExecutor(step, stepContext);
        
        _logger.LogInformation("Running step: {StepName}", step.Name);
        var result = await executor.Run();
        
        if (result.Failed)
        {
            _logger.LogError("Step {StepName} failed with exit code {ExitCode}", 
                step.Name, result.ExitCode);
        }
        else
        {
            _logger.LogDebug("Step {StepName} completed successfully", step.Name);
        }

        return result;
    }
}