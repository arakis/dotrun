using DotRun.Runtime.Model;

namespace DotRun.Runtime.Services.Tracking;

public interface IWorkflowTrackingService
{
    Task<Guid> EnsureWorkflowEntityAsync(string projectName, string workflowName);
    
    Task<Guid> CreateWorkflowRunAsync(Guid workflowId, string name, string slug, string source);
    Task StartWorkflowRunAsync(Guid runId);
    Task CompleteWorkflowRunAsync(Guid runId, ExecutionStatus status);
    
    Task CreateExecutionPlanAsync(Guid workflowRunId, Guid workflowId, WorkflowContext workflowContext);
}