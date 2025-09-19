using System;
using System.Threading;
using System.Threading.Tasks;
using DotRun.Runtime.Runners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime.Services;

public class WorkflowExecutor : IWorkflowExecutor
{
    private readonly ILogger<WorkflowExecutor> _logger;
    private readonly IProjectResolver _projectResolver;
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IServiceProvider _serviceProvider;

    public WorkflowExecutor(
        ILogger<WorkflowExecutor> logger,
        IProjectResolver projectResolver,
        IWorkflowRunner workflowRunner,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _projectResolver = projectResolver;
        _workflowRunner = workflowRunner;
        _serviceProvider = serviceProvider;
    }

    public async Task<WorkflowResult> ExecuteAsync(
        DotRunConfig config,
        string projectName,
        string workflowName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting workflow execution: Project={Project}, Workflow={Workflow}", 
                projectName, workflowName);

            // Validate project and workflow exist
            if (!_projectResolver.ValidateProject(config, projectName))
            {
                _logger.LogError("Project not found: {Project}", projectName);
                throw new InvalidOperationException($"Project '{projectName}' not found");
            }

            var project = _projectResolver.GetProject(config, projectName);
            
            if (!_projectResolver.ValidateWorkflow(project, workflowName))
            {
                _logger.LogError("Workflow not found: {Workflow} in project {Project}", 
                    workflowName, projectName);
                throw new InvalidOperationException($"Workflow '{workflowName}' not found in project '{projectName}'");
            }

            var workflow = _projectResolver.GetWorkflow(project, workflowName);

            // Create run context (it manages its own scope)
            using var runContext = new RunContext(_serviceProvider);

            // Execute workflow with cancellation support
            using (_logger.BeginScope("WorkflowExecution[{WorkflowId}]", Guid.NewGuid()))
            {
                _logger.LogDebug("Executing workflow with {JobCount} jobs", workflow.Jobs?.Count ?? 0);

                // Use WorkflowRunner instead of calling workflow.Run directly
                var workflowTask = _workflowRunner.RunAsync(workflow, runContext);
                
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
}