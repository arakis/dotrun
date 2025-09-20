namespace DotRun.Database;

public record WorkflowEntity : EntityBase
{
    public Guid ProjectId { get; set; }
    public string Slug { get; set; }
    public string Name { get; set; }
    public DateTimeOffset? LastRunAt { get; set; }
    public bool IsActive { get; set; } = true;

    // --- Navigation properties ---

    public ProjectEntity Project { get; set; } = null!;
}