namespace DotRun.Database;

public record WorkflowRecord : RecordBase
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; }

    // --- Navigation properties ---

    public ProjectRecord Project { get; set; } = null!;
}