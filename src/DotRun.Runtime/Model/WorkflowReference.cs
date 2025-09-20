namespace DotRun.Runtime;

public record WorkflowReference
{
    public WorkflowReference()
    {
    }

    public WorkflowReference(string repository, string? path, string workflow)
    {
        Repository = repository;
        Path = path;
        Workflow = workflow;
    }

    public WorkflowReference(string repository, string workflow)
    {
        Repository = repository;
        Path = null;
        Workflow = workflow;
    }

    public required string Repository { get; init; }
    public required string? Path { get; init; }
    public required string Workflow { get; init; }

    public override string ToString()
        => $"{Repository + "/"}{(string.IsNullOrEmpty(Path) ? "" : Path + "/")}{Workflow}";

    public static WorkflowReference Parse(string path)
    {
        var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            throw new ArgumentException("Invalid workflow reference", nameof(path));

        if (parts.Length == 1)
        {
            return new WorkflowReference
            {
                Repository = "",
                Path = null,
                Workflow = parts[0]
            };
        }
        else if (parts.Length == 2)
        {
            return new WorkflowReference
            {
                Repository = parts[0],
                Path = null,
                Workflow = parts[1]
            };
        }
        else
        {
            return new WorkflowReference
            {
                Repository = parts[0],
                Path = string.Join('/', parts, 1, parts.Length - 2),
                Workflow = parts[^1]
            };
        }
    }
}