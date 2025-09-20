using System.Threading.Tasks;
using DotRun.Runtime.Factories;
using Microsoft.Extensions.Logging;
using DotRun.Runtime.Services.Tracking;
using DotRun.Runtime.Model;

namespace DotRun.Runtime.Runners;

public class StepRunner : IStepRunner
{
    private readonly IStepTrackingService _trackingService;
    private readonly IStepExecutorFactory _stepExecutorFactory;
    private readonly ILogger<StepRunner> _logger;

    public StepRunner(
        IStepTrackingService trackingService,
        IStepExecutorFactory stepExecutorFactory,
        ILogger<StepRunner> logger)
    {
        _trackingService = trackingService;
        _stepExecutorFactory = stepExecutorFactory;
        _logger = logger;
    }

    public async Task<StepResult> RunAsync(Step step, StepContext stepContext)
    {
        _logger.LogInformation("Starting step: {StepName}", step.Name);
        
        await _trackingService.StartStepRunAsync(stepContext.StepRunId);

        var executor = _stepExecutorFactory.CreateStepExecutor(step, stepContext);
        var result = await executor.Run();

        var status = result.Failed ? ExecutionStatus.Failed : ExecutionStatus.Succeeded;
        await _trackingService.CompleteStepRunAsync(stepContext.StepRunId, status);

        return result;
    }
}