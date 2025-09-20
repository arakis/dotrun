namespace DotRun.Database;

public record WorkflowEntity : EntityBase
{
    public string Slug { get; set; }
    public string Name { get; set; }
    public string QualifiedPath { get; set; }
    public string YamlFilePath { get; set; }
    public string SourceHash { get; set; }
    public DateTimeOffset? LastRunAt { get; set; }
    public DateTimeOffset? DeactivatedAt { get; set; }
}