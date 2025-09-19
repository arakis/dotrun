using System.Linq;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime.Services;

public class ProjectResolver : IProjectResolver
{
    private readonly ILogger<ProjectResolver> _logger;

    public ProjectResolver(ILogger<ProjectResolver> logger)
    {
        _logger = logger;
    }

    public Project GetProject(DotRunConfig config, string projectName)
    {
        _logger.LogDebug("Resolving project: {ProjectName}", projectName);
        
        if (config?.Projects == null)
        {
            _logger.LogError("Configuration or projects collection is null");
            return null;
        }

        var project = config.Projects[projectName];
        if (project == null)
        {
            _logger.LogError("Project not found: {ProjectName}", projectName);
            return null;
        }
        _logger.LogDebug("Project resolved: {ProjectName} with {WorkflowCount} workflows", 
            projectName, project.Workflows?.Count ?? 0);
        
        return project;
    }

    public Workflow GetWorkflow(Project project, string workflowName)
    {
        _logger.LogDebug("Resolving workflow: {WorkflowName}", workflowName);
        
        if (project?.Workflows == null)
        {
            _logger.LogError("Project or workflows collection is null");
            return null;
        }

        var workflow = project.Workflows[workflowName];
        if (workflow == null)
        {
            _logger.LogError("Workflow not found: {WorkflowName}", workflowName);
            return null;
        }
        _logger.LogDebug("Workflow resolved: {WorkflowName} with {JobCount} jobs", 
            workflowName, workflow.Jobs?.Count ?? 0);
        
        return workflow;
    }

    public bool ValidateProject(DotRunConfig config, string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            _logger.LogError("Project name is null or empty");
            return false;
        }

        if (config?.Projects == null)
        {
            _logger.LogError("Configuration or projects collection is null");
            return false;
        }

        bool exists = config.Projects[projectName] != null;
        
        if (!exists)
        {
            _logger.LogWarning("Project validation failed: {ProjectName} not found", projectName);
            _logger.LogDebug("Available projects: {Projects}", 
                string.Join(", ", config.Projects.Select(p => p.Name)));
        }

        return exists;
    }

    public bool ValidateWorkflow(Project project, string workflowName)
    {
        if (string.IsNullOrWhiteSpace(workflowName))
        {
            _logger.LogError("Workflow name is null or empty");
            return false;
        }

        if (project?.Workflows == null)
        {
            _logger.LogError("Project or workflows collection is null");
            return false;
        }

        bool exists = project.Workflows[workflowName] != null;
        
        if (!exists)
        {
            _logger.LogWarning("Workflow validation failed: {WorkflowName} not found", workflowName);
            _logger.LogDebug("Available workflows: {Workflows}", 
                string.Join(", ", project.Workflows.Select(w => w.Name)));
        }

        return exists;
    }
}