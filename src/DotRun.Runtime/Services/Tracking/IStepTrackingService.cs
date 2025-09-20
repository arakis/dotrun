using DotRun.Runtime.Model;

namespace DotRun.Runtime.Services.Tracking;

public interface IStepTrackingService
{
    Task CreateStepRunAsync(Guid stepRunId, Guid jobRunId, string name);
    Task StartStepRunAsync(Guid stepRunId);
    Task CompleteStepRunAsync(Guid stepRunId, ExecutionStatus status);
    Task SkipStepRunAsync(Guid stepRunId);
}