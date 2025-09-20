namespace DotRun.Database;

public record WorkflowRunRecord : RecordBase
{
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// Original source of the workflow run (e.g. YAML definition)
    /// </summary>
    public string Source { get; set; } = null!;
    
    public ExecutionStatus Status { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}