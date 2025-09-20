using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DotRun.WebServer.Models;
using DotRun.Database;
using System.Linq;

namespace DotRun.WebServer.Controllers;

[Route("workflows")]
public class WorkflowController : Controller
{
    private readonly ILogger<WorkflowController> _logger;
    private readonly DotRunDbContext _dbContext;

    public WorkflowController(ILogger<WorkflowController> logger, DotRunDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [HttpGet("")]
    [HttpGet("{**path}")]
    public async Task<IActionResult> Browse(string? path = null)
    {
        path = path ?? "";
        
        // Query all active workflows from the database
        var workflows = await _dbContext.Workflow
            .Where(w => w.DeactivatedAt == null)
            .OrderBy(w => w.QualifiedPath)
            .ToListAsync();

        // If path ends with a workflow name (no extension), show the workflow detail
        var matchingWorkflow = workflows.FirstOrDefault(w => 
            w.QualifiedPath.Equals(path, StringComparison.OrdinalIgnoreCase) ||
            (path.Contains('.') && w.YamlFilePath.EndsWith(path, StringComparison.OrdinalIgnoreCase)));

        if (matchingWorkflow != null)
        {
            return await ShowWorkflow(matchingWorkflow);
        }

        // Otherwise, show directory listing
        return ShowDirectory(workflows, path);
    }

    private IActionResult ShowDirectory(List<WorkflowEntity> workflows, string currentPath)
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
        
        var relevantWorkflows = workflows.Where(w =>
        {
            if (string.IsNullOrEmpty(currentPath))
                return true;
            
            return w.QualifiedPath.StartsWith(currentPath + "/", StringComparison.OrdinalIgnoreCase) ||
                   w.QualifiedPath.Equals(currentPath, StringComparison.OrdinalIgnoreCase);
        }).ToList();

        // Get unique directories at this level
        var directories = new HashSet<string>();
        var files = new List<WorkflowItemViewModel>();

        foreach (var workflow in relevantWorkflows)
        {
            var workflowPathParts = workflow.QualifiedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
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
                var workflowName = Path.GetFileNameWithoutExtension(workflow.YamlFilePath);
                files.Add(new WorkflowItemViewModel
                {
                    Name = workflowName,
                    IsDirectory = false,
                    Path = workflow.QualifiedPath
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

    private async Task<IActionResult> ShowWorkflow(WorkflowEntity workflow)
    {
        string workflowContent = "";
        
        // Try to read the workflow file content if the file exists
        if (!string.IsNullOrEmpty(workflow.YamlFilePath) && System.IO.File.Exists(workflow.YamlFilePath))
        {
            try
            {
                workflowContent = await System.IO.File.ReadAllTextAsync(workflow.YamlFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read workflow file at {Path}", workflow.YamlFilePath);
                workflowContent = "# Workflow file not accessible\n# Path: " + workflow.YamlFilePath;
            }
        }
        else
        {
            workflowContent = "# Workflow file not found\n# Expected path: " + workflow.YamlFilePath;
        }
        
        var model = new WorkflowDetailViewModel
        {
            Name = workflow.Name,
            Path = workflow.QualifiedPath,
            Content = workflowContent,
            BreadcrumbItems = BuildBreadcrumbs(workflow.QualifiedPath)
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