using Microsoft.AspNetCore.Mvc;
using DotRun.WebServer.Models;
using DotRun.WebServer.Services;
using DotRun.Runtime;
using DotRun.Runtime.Services;
using System.Linq;

namespace DotRun.WebServer.Controllers;

[Route("workflows")]
public class WorkflowController : Controller
{
    private readonly ILogger<WorkflowController> _logger;
    private readonly IWorkflowResolver _workflowResolver;
    private readonly IDotRunConfigService _configService;

    public WorkflowController(
        ILogger<WorkflowController> logger, 
        IWorkflowResolver workflowResolver,
        IDotRunConfigService configService)
    {
        _logger = logger;
        _workflowResolver = workflowResolver;
        _configService = configService;
    }

    [HttpGet("")]
    [HttpGet("{**path}")]
    public IActionResult Browse(string? path = null)
    {
        path = path ?? "";
        
        // Get configuration and list all workflows dynamically
        var config = _configService.GetConfiguration();
        var mountPaths = _workflowResolver.ListWorkflowMountPaths(config);
        var workflowPaths = mountPaths.Select(mp => mp.MountPath).ToList();
        
        // Check if the path matches a workflow
        if (mountPaths.Any(mp => mp.MountPath == path))
        {
            return ShowWorkflow(path);
        }
        
        // Otherwise, show directory listing
        return ShowDirectory(workflowPaths, path);
    }

    private IActionResult ShowDirectory(List<string> workflowPaths, string currentPath)
    {
        var model = new WorkflowBrowserViewModel
        {
            CurrentPath = currentPath,
            BreadcrumbItems = BuildBreadcrumbs(currentPath),
            Items = new List<WorkflowItemViewModel>()
        };

        // Build a virtual file system from qualified paths
        var pathSegments = string.IsNullOrEmpty(currentPath) 
            ? Array.Empty<string>() 
            : currentPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        var relevantWorkflows = workflowPaths.Where(w =>
        {
            if (string.IsNullOrEmpty(currentPath))
                return true;
            
            return w.StartsWith(currentPath + "/", StringComparison.OrdinalIgnoreCase) ||
                   w.Equals(currentPath, StringComparison.OrdinalIgnoreCase);
        }).ToList();

        // Get unique directories at this level
        var directories = new HashSet<string>();
        var files = new List<WorkflowItemViewModel>();

        foreach (var workflowPath in relevantWorkflows)
        {
            var workflowPathParts = workflowPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            // If workflow path has more segments than current path, it might be in a subdirectory
            if (workflowPathParts.Length > pathSegments.Length + 1)
            {
                // This is in a subdirectory - add the next directory segment
                var nextDir = workflowPathParts[pathSegments.Length];
                directories.Add(nextDir);
            }
            else if (workflowPathParts.Length == pathSegments.Length + 1 || 
                     (pathSegments.Length == 0 && workflowPathParts.Length > 0))
            {
                // This is a direct child workflow
                var workflowName = workflowPathParts.Last();
                files.Add(new WorkflowItemViewModel
                {
                    Name = workflowName,
                    IsDirectory = false,
                    Path = workflowPath
                });
            }
        }

        // Add directories to model
        foreach (var dir in directories.OrderBy(d => d))
        {
            var dirPath = string.IsNullOrEmpty(currentPath) 
                ? dir 
                : $"{currentPath}/{dir}";
                
            model.Items.Add(new WorkflowItemViewModel
            {
                Name = dir,
                IsDirectory = true,
                Path = dirPath
            });
        }

        // Add files to model
        model.Items.AddRange(files.OrderBy(f => f.Name));

        return View("Browse", model);
    }

    private IActionResult ShowWorkflow(string workflowPath)
    {
        var config = _configService.GetConfiguration();
        string workflowContent = "";
        string workflowName = workflowPath;
        
        try
        {
            // Get the workflow using the resolver
            var (workflow, reference) = _workflowResolver.GetWorkflowByMountPath(config, workflowPath);
            var qualifiedPath = reference?.ToString();
            
            if (workflow != null)
            {
                // Extract workflow name from path
                var pathParts = workflowPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                workflowName = pathParts.Length > 0 ? pathParts.Last() : workflowPath;
                
                // Try to read the actual YAML file content if available
                if (!string.IsNullOrEmpty(workflow.WorkflowFilePath) && System.IO.File.Exists(workflow.WorkflowFilePath))
                {
                    try
                    {
                        workflowContent = System.IO.File.ReadAllText(workflow.WorkflowFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not read workflow file at {Path}", workflow.WorkflowFilePath);
                        workflowContent = $"# Workflow file not accessible\n# Path: {workflow.WorkflowFilePath}\n# Error: {ex.Message}";
                    }
                }
                else
                {
                    // Fallback: Show workflow information
                    workflowContent = $"# Workflow: {workflowName}\n";
                    workflowContent += $"# Qualified Path: {qualifiedPath}\n";
                    
                    if (!string.IsNullOrEmpty(workflow.WorkflowFilePath))
                    {
                        workflowContent += $"# File Path: {workflow.WorkflowFilePath} (not found)\n";
                    }
                    
                    workflowContent += $"# Jobs: {workflow.Jobs?.Count ?? 0}\n";
                    
                    if (workflow.Jobs != null && workflow.Jobs.Count > 0)
                    {
                        workflowContent += "\njobs:\n";
                        foreach (var job in workflow.Jobs)
                        {
                            workflowContent += $"  {job.Name ?? "unnamed"}:\n";
                            if (job.Steps != null)
                            {
                                workflowContent += $"    # {job.Steps.Count} steps\n";
                            }
                        }
                    }
                }
            }
            else
            {
                workflowContent = $"# Workflow not found: {workflowPath}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading workflow {Path}", workflowPath);
            workflowContent = $"# Error loading workflow\n# {ex.Message}";
        }
        
        var model = new WorkflowDetailViewModel
        {
            Name = workflowName,
            Path = workflowPath,
            Content = workflowContent,
            BreadcrumbItems = BuildBreadcrumbs(workflowPath)
        };

        return View("Detail", model);
    }

    private List<BreadcrumbItem> BuildBreadcrumbs(string path)
    {
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Name = "Workflows", Path = "" }
        };

        if (string.IsNullOrEmpty(path))
            return breadcrumbs;

        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var currentPath = "";

        for (int i = 0; i < parts.Length; i++)
        {
            if (!string.IsNullOrEmpty(currentPath))
                currentPath += "/";
            
            currentPath += parts[i];
            
            breadcrumbs.Add(new BreadcrumbItem
            {
                Name = parts[i],
                Path = currentPath
            });
        }

        return breadcrumbs;
    }
}