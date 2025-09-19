namespace DotRun.Runtime.Services;

public interface IProjectResolver
{
    Project GetProject(DotRunConfig config, string projectName);
    Workflow GetWorkflow(Project project, string workflowName);
    bool ValidateProject(DotRunConfig config, string projectName);
    bool ValidateWorkflow(Project project, string workflowName);
}