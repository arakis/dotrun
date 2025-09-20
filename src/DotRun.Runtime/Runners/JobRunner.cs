using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DotRun.Runtime.Services.Tracking;
using DotRun.Runtime.Model;

namespace DotRun.Runtime.Runners;

public class JobRunner : IJobRunner
{
    private readonly IStepRunner _stepRunner;
    private readonly IJobTrackingService _trackingService;
    private readonly ILogger<JobRunner> _logger;

    public JobRunner(
        IStepRunner stepRunner,
        IJobTrackingService trackingService,
        ILogger<JobRunner> logger)
    {
        _stepRunner = stepRunner;
        _trackingService = trackingService;
        _logger = logger;
    }

    public async Task<JobResult> RunAsync(Job job, JobContext jobContext)
    {
        _logger.LogInformation("Starting job: {JobName}", job.Name);
        
        await _trackingService.StartJobRunAsync(jobContext.JobRunId);

        var results = new List<StepResult>();

        // Handle nested jobs
        if (jobContext.SubJobContexts != null && jobContext.SubJobContexts.Count > 0)
        {
            foreach (var subJobContext in jobContext.SubJobContexts)
            {
                var subJobResult = await RunAsync(subJobContext.Job, subJobContext);
                
                results.Add(new StepResult(null) { ExitCode = subJobResult.Failed ? 1 : 0 });
                
                if (subJobResult.Failed) break;
            }
        }
        // Handle steps
        else if (jobContext.StepContexts != null && jobContext.StepContexts.Count > 0)
        {
            foreach (var stepContext in jobContext.StepContexts)
            {
                var result = await _stepRunner.RunAsync(stepContext.Step, stepContext);
                results.Add(result);

                if (result.Failed) break;
            }
        }

        var jobResult = new JobResult(jobContext) { StepResults = results };
        
        var status = jobResult.Failed ? ExecutionStatus.Failed : ExecutionStatus.Succeeded;
        await _trackingService.CompleteJobRunAsync(jobContext.JobRunId, status);

        return jobResult;
    }
}