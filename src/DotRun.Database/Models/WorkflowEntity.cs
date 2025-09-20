namespace DotRun.Database;

public record WorkflowEntity : EntityBase
{
    public Guid ProjectId { get; set; }
    public string Slug { get; set; }
    public string Name { get; set; }

    // --- Navigation properties ---

    public ProjectEntity Project { get; set; } = null!;
}