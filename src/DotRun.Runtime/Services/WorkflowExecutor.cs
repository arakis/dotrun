using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DotRun.Runtime.Runners;
using DotRun.Runtime.Services.Tracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime.Services;

public class WorkflowExecutor : IWorkflowExecutor
{
    private readonly ILogger<WorkflowExecutor> _logger;
    private readonly IProjectResolver _projectResolver;
    private readonly IServiceProvider _serviceProvider;

    public WorkflowExecutor(
        ILogger<WorkflowExecutor> logger,
        IProjectResolver projectResolver,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _projectResolver = projectResolver;
        _serviceProvider = serviceProvider;
    }

    public async Task<WorkflowResult> ExecuteAsync(
        DotRunConfigLegacy configLegacy,
        string projectName,
        string workflowName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting workflow execution: Project={Project}, Workflow={Workflow}",
                projectName, workflowName);

            // Validate project and workflow exist
            if (!_projectResolver.ValidateProject(configLegacy, projectName))
            {
                _logger.LogError("Project not found: {Project}", projectName);
                throw new InvalidOperationException($"Project '{projectName}' not found");
            }

            var project = _projectResolver.GetProject(configLegacy, projectName);

            if (!_projectResolver.ValidateWorkflow(project, workflowName))
            {
                _logger.LogError("Workflow not found: {Workflow} in project {Project}",
                    workflowName, projectName);
                throw new InvalidOperationException($"Workflow '{workflowName}' not found in project '{projectName}'");
            }

            var workflow = _projectResolver.GetWorkflow(project, workflowName);

            // Create run context with its own scope
            using var runContext = new RunContext(_serviceProvider);

            // Get scoped tracking service from the RunContext's scope
            var scopedTracking = runContext.ServiceProvider.GetRequiredService<IWorkflowTrackingService>();

            // Ensure workflow entity exists
            var workflowId = await scopedTracking.EnsureWorkflowEntityAsync(projectName, workflowName);

            // Get workflow source (YAML content)
            var yamlSource = File.ReadAllText(workflow.WorkflowFilePath);

            // Create workflow run
            var workflowRunId = await scopedTracking.CreateWorkflowRunAsync(
                workflowId, workflow.Name, workflow.Name, yamlSource);

            // Build the entire context tree with IDs upfront
            var workflowContext = BuildWorkflowContext(workflow, workflowRunId, runContext);

            // Pre-create all execution records using the context tree
            await scopedTracking.CreateExecutionPlanAsync(workflowRunId, workflowId, workflowContext);

            // Get scoped runner from the RunContext's scope
            var scopedRunner = runContext.ServiceProvider.GetRequiredService<IWorkflowRunner>();

            // Execute workflow with the pre-built context
            using (_logger.BeginScope("WorkflowExecution[{WorkflowRunId}]", workflowRunId))
            {
                _logger.LogDebug("Executing workflow with {JobCount} jobs", workflow.Jobs?.Count ?? 0);

                var workflowTask = scopedRunner.RunAsync(workflow, workflowContext);

                // Handle cancellation
                var completedTask = await Task.WhenAny(
                    workflowTask,
                    Task.Delay(Timeout.Infinite, cancellationToken));

                if (completedTask != workflowTask)
                {
                    _logger.LogWarning("Workflow execution cancelled");
                    throw new OperationCanceledException("Workflow execution was cancelled");
                }

                var result = await workflowTask;

                if (result.Failed)
                {
                    _logger.LogError("Workflow execution failed");
                }
                else
                {
                    _logger.LogInformation("Workflow execution completed successfully");
                }

                return result;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Workflow execution was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow execution failed with exception");
            throw;
        }
    }

    private WorkflowContext BuildWorkflowContext(Workflow workflow, Guid workflowRunId, RunContext runContext)
    {
        var workflowContext = new WorkflowContext(workflow, runContext)
        {
            WorkflowRunId = workflowRunId,
            JobContexts = new List<JobContext>()
        };

        foreach (var job in workflow.Jobs)
        {
            var jobContext = BuildJobContext(job, workflowContext);
            workflowContext.JobContexts.Add(jobContext);
        }

        return workflowContext;
    }

    private JobContext BuildJobContext(Job job, WorkflowContext workflowContext)
    {
        var jobContext = new JobContext(job, workflowContext)
        {
            JobRunId = Guid.NewGuid()
        };

        // Handle nested jobs
        if (job.Jobs != null && job.Jobs.Count > 0)
        {
            jobContext.SubJobContexts = new List<JobContext>();
            foreach (var subJob in job.Jobs)
            {
                var subJobContext = BuildJobContext(subJob, workflowContext);
                jobContext.SubJobContexts.Add(subJobContext);
            }
        }
        // Handle steps
        else if (job.Steps != null && job.Steps.Count > 0)
        {
            jobContext.StepContexts = new List<StepContext>();
            foreach (var step in job.Steps)
            {
                var stepContext = new StepContext(step, jobContext)
                {
                    StepRunId = Guid.NewGuid()
                };
                jobContext.StepContexts.Add(stepContext);
            }
        }

        return jobContext;
    }
}