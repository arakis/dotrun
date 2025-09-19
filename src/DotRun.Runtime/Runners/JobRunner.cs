using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime.Runners;

public class JobRunner : IJobRunner
{
    private readonly IStepRunner _stepRunner;
    private readonly ILogger<JobRunner> _logger;

    public JobRunner(
        IStepRunner stepRunner,
        ILogger<JobRunner> logger)
    {
        _stepRunner = stepRunner;
        _logger = logger;
    }

    public async Task<JobResult> RunAsync(Job job, JobContext jobContext)
    {
        _logger.LogInformation("Starting job: {JobName}", job.Name);

        var results = new List<StepResult>();

        // Handle nested jobs
        if (job.Jobs != null && job.Jobs.Count > 0)
        {
            _logger.LogDebug("Job {JobName} has {SubJobCount} sub-jobs", job.Name, job.Jobs.Count);
            
            foreach (var subJob in job.Jobs)
            {
                var subJobContext = new JobContext(subJob, jobContext.WorkflowContext);
                var subJobResult = await RunAsync(subJob, subJobContext);
                
                // Convert sub-job result to step result for compatibility
                results.Add(new StepResult(null) { ExitCode = subJobResult.Failed ? 1 : 0 });
                
                if (subJobResult.Failed)
                {
                    _logger.LogError("Sub-job {SubJobName} failed in job {JobName}", subJob.Name, job.Name);
                    break;
                }
            }
        }
        // Handle steps
        else if (job.Steps != null && job.Steps.Count > 0)
        {
            _logger.LogDebug("Job {JobName} has {StepCount} steps", job.Name, job.Steps.Count);
            
            foreach (var step in job.Steps)
            {
                var stepContext = new StepContext(step, jobContext);
                _logger.LogDebug("Running step: {StepName}", step.Name);
                
                var result = await _stepRunner.RunAsync(step, stepContext);
                results.Add(result);

                if (result.Failed)
                {
                    _logger.LogError("Step {StepName} failed in job {JobName}", step.Name, job.Name);
                    break;
                }
            }
        }

        var jobResult = new JobResult(jobContext)
        {
            StepResults = results
        };

        if (jobResult.Failed)
        {
            _logger.LogError("Job {JobName} failed", job.Name);
        }
        else
        {
            _logger.LogInformation("Job {JobName} completed successfully", job.Name);
        }

        return jobResult;
    }
}