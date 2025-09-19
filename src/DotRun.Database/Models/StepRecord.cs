namespace DotRun.Database;

public record StepRecord : RecordBase
{
    public Guid JobId { get; set; }
    public string Name { get; set; }

    // --- Navigation properties ---

    public JobRecord Job { get; set; } = null!;
}