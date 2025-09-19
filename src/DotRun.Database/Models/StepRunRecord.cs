namespace DotRun.Database;

public record StepRunRecord : RecordBase
{
    public Guid StepId { get; set; }
    
    public Guid JobId { get; set; }
    public Guid JobRunId { get; set; }
    public ExecutionStatus Status { get; set; }

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}