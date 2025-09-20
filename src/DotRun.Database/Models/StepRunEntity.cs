using DotRun.Runtime.Model;

namespace DotRun.Database;

public record StepRunEntity : EntityBase
{
    public string Slug { get; set; }
    public string Name { get; set; }
    
    public Guid JobRunId { get; set; }
    public ExecutionStatus Status { get; set; }

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}