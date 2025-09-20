namespace DotRun.Database;

public record ProjectEntity : EntityBase
{
    public string Name { get; set; }
    public string Slug { get; set; }
}