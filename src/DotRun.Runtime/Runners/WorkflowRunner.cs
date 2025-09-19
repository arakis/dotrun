using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime.Runners;

public class WorkflowRunner : IWorkflowRunner
{
    private readonly IJobRunner _jobRunner;
    private readonly ILogger<WorkflowRunner> _logger;

    public WorkflowRunner(
        IJobRunner jobRunner,
        ILogger<WorkflowRunner> logger)
    {
        _jobRunner = jobRunner;
        _logger = logger;
    }

    public async Task<WorkflowResult> RunAsync(Workflow workflow, RunContext runContext)
    {
        _logger.LogInformation("Starting workflow: {WorkflowName}", workflow.Name);

        var workflowContext = new WorkflowContext(workflow, runContext);
        var results = new List<JobResult>();

        foreach (var job in workflow.Jobs)
        {
            var jobContext = new JobContext(job, workflowContext);
            _logger.LogDebug("Running job: {JobName}", job.Name);
            
            var result = await _jobRunner.RunAsync(job, jobContext);
            results.Add(result);

            if (result.Failed)
            {
                _logger.LogError("Job {JobName} failed, stopping workflow", job.Name);
                break;
            }
        }

        var workflowResult = new WorkflowResult(workflowContext)
        {
            JobResults = results
        };

        if (workflowResult.Failed)
        {
            _logger.LogError("Workflow {WorkflowName} failed", workflow.Name);
        }
        else
        {
            _logger.LogInformation("Workflow {WorkflowName} completed successfully", workflow.Name);
        }

        return workflowResult;
    }
}