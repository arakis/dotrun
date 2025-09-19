namespace DotRun.Database;

public record WorkflowRunRecord : RecordBase
{
    public Guid WorkflowId { get; set; }
    
    public ExecutionStatus Status { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}