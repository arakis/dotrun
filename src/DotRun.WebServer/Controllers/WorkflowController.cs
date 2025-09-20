using Microsoft.AspNetCore.Mvc;
using DotRun.WebServer.Models;
using System.IO;

namespace DotRun.WebServer.Controllers;

[Route("workflows")]
public class WorkflowController : Controller
{
    private readonly ILogger<WorkflowController> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _workflowBasePath;

    public WorkflowController(ILogger<WorkflowController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _workflowBasePath = _configuration["WorkflowBasePath"] ?? "/home/sebastian/dotrun.config";
    }

    [HttpGet("")]
    [HttpGet("{**path}")]
    public IActionResult Browse(string? path = null)
    {
        path = path ?? "";
        
        var fullPath = Path.Combine(_workflowBasePath, path);
        
        // Security check: ensure we're not going outside the workflow directory
        var resolvedPath = Path.GetFullPath(fullPath);
        if (!resolvedPath.StartsWith(Path.GetFullPath(_workflowBasePath)))
        {
            return NotFound();
        }

        // If it's a yaml file, display the workflow
        if (System.IO.File.Exists(resolvedPath) && (Path.GetExtension(resolvedPath) == ".yaml" || Path.GetExtension(resolvedPath) == ".yml"))
        {
            return ShowWorkflow(resolvedPath, path);
        }

        // If it's a directory, show the file browser
        if (Directory.Exists(resolvedPath))
        {
            return ShowDirectory(resolvedPath, path);
        }

        return NotFound();
    }

    private IActionResult ShowDirectory(string fullPath, string relativePath)
    {
        var model = new WorkflowBrowserViewModel
        {
            CurrentPath = relativePath,
            BreadcrumbItems = BuildBreadcrumbs(relativePath),
            Items = new List<WorkflowItemViewModel>()
        };

        // Get directories
        foreach (var dir in Directory.GetDirectories(fullPath))
        {
            var dirName = Path.GetFileName(dir);
            model.Items.Add(new WorkflowItemViewModel
            {
                Name = dirName,
                IsDirectory = true,
                Path = string.IsNullOrEmpty(relativePath) 
                    ? dirName 
                    : Path.Combine(relativePath, dirName).Replace('\\', '/')
            });
        }

        // Get workflow files
        foreach (var file in Directory.GetFiles(fullPath, "*.yaml")
                    .Concat(Directory.GetFiles(fullPath, "*.yml")))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var fullFileName = Path.GetFileName(file);
            model.Items.Add(new WorkflowItemViewModel
            {
                Name = fileName,
                IsDirectory = false,
                Path = string.IsNullOrEmpty(relativePath) 
                    ? fullFileName
                    : Path.Combine(relativePath, fullFileName).Replace('\\', '/')
            });
        }

        // Sort: directories first, then files, alphabetically
        model.Items = model.Items
            .OrderByDescending(i => i.IsDirectory)
            .ThenBy(i => i.Name)
            .ToList();

        return View("Browse", model);
    }

    private IActionResult ShowWorkflow(string fullPath, string relativePath)
    {
        var workflowContent = System.IO.File.ReadAllText(fullPath);
        var fileName = Path.GetFileNameWithoutExtension(fullPath);
        
        var model = new WorkflowDetailViewModel
        {
            Name = fileName,
            Path = relativePath,
            Content = workflowContent,
            BreadcrumbItems = BuildBreadcrumbs(relativePath)
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

            var isFile = i == parts.Length - 1 && 
                        (parts[i].EndsWith(".yaml") || parts[i].EndsWith(".yml"));
            
            breadcrumbs.Add(new BreadcrumbItem
            {
                Name = isFile ? Path.GetFileNameWithoutExtension(parts[i]) : parts[i],
                Path = currentPath
            });
        }

        return breadcrumbs;
    }
}