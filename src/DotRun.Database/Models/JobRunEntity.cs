namespace DotRun.Database;

public record JobRunEntity : EntityBase
{
    public string Name { get; set; }

    public Guid WorkflowId { get; set; }
    public Guid WorkflowRunId { get; set; }

    public ExecutionStatus Status { get; set; }

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}