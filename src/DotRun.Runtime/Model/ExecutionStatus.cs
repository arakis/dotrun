namespace DotRun.Runtime.Model;

public enum ExecutionStatus
{
    Queued,
    Pending,
    Started,
    Failed,
    Succeeded,
    Cancelled,
    Skipped
}