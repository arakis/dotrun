using DotRun.Runtime.Model;

namespace DotRun.Runtime.Services.Tracking;

public interface IJobTrackingService
{
    Task CreateJobRunAsync(Guid jobRunId, Guid workflowRunId, Guid workflowId, string name);
    Task StartJobRunAsync(Guid jobRunId);
    Task CompleteJobRunAsync(Guid jobRunId, ExecutionStatus status);
    Task SkipJobRunAsync(Guid jobRunId);
}