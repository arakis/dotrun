namespace DotRun.Runtime.Services;

public interface IProjectResolver
{
    Project GetProject(DotRunConfigLegacy configLegacy, string projectName);
    Workflow GetWorkflow(Project project, string workflowName);
    bool ValidateProject(DotRunConfigLegacy configLegacy, string projectName);
    bool ValidateWorkflow(Project project, string workflowName);
}