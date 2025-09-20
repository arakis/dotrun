using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DotRun.Runtime.Services.Tracking;
using DotRun.Runtime.Model;

namespace DotRun.Runtime.Runners;

public class WorkflowRunner : IWorkflowRunner
{
    private readonly IJobRunner _jobRunner;
    private readonly IWorkflowTrackingService _trackingService;
    private readonly ILogger<WorkflowRunner> _logger;

    public WorkflowRunner(
        IJobRunner jobRunner,
        IWorkflowTrackingService trackingService,
        ILogger<WorkflowRunner> logger)
    {
        _jobRunner = jobRunner;
        _trackingService = trackingService;
        _logger = logger;
    }

    public async Task<WorkflowResult> RunAsync(Workflow workflow, WorkflowContext workflowContext)
    {
        _logger.LogInformation("Starting workflow: {WorkflowName}", workflow.Name);
        
        await _trackingService.StartWorkflowRunAsync(workflowContext.WorkflowRunId);

        var results = new List<JobResult>();

        foreach (var jobContext in workflowContext.JobContexts)
        {
            var result = await _jobRunner.RunAsync(jobContext.Job, jobContext);
            results.Add(result);

            if (result.Failed)
            {
                break;
            }
        }

        var status = results.Any(r => r.Failed) ? ExecutionStatus.Failed : ExecutionStatus.Succeeded;
        await _trackingService.CompleteWorkflowRunAsync(workflowContext.WorkflowRunId, status);

        return new WorkflowResult(workflowContext) { JobResults = results };
    }
}