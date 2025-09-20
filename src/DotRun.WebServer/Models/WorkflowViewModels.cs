namespace DotRun.WebServer.Models;

public class WorkflowBrowserViewModel
{
    public string CurrentPath { get; set; } = "";
    public List<BreadcrumbItem> BreadcrumbItems { get; set; } = new();
    public List<WorkflowItemViewModel> Items { get; set; } = new();
}

public class WorkflowItemViewModel
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public bool IsDirectory { get; set; }
}

public class WorkflowDetailViewModel
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Content { get; set; } = "";
    public List<BreadcrumbItem> BreadcrumbItems { get; set; } = new();
}

public class BreadcrumbItem
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
}