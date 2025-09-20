using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotRun.Runtime.Model.V2;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime.Services.V2;

public class WorkflowResolver : IWorkflowResolver
{
    private readonly ILogger<WorkflowResolver> _logger;

    public WorkflowResolver(ILogger<WorkflowResolver> logger)
    {
        _logger = logger;
    }

    public Workflow GetByPath(DotRunConfigV2 config, WorkflowReference reference)
    {
        throw new NotImplementedException();
    }

    public (Workflow Workflow, string QualifiedPath) GetByPath(DotRunConfigV2 config, string workflowPath)
    {
        _logger.LogDebug("Resolving workflow: {WorkflowPath}", workflowPath);

        if (string.IsNullOrWhiteSpace(workflowPath))
        {
            _logger.LogError("Workflow path is null or empty");
            return (null, null);
        }

        // Try direct resolution first (fully qualified path)
        var directResult = TryDirectResolution(config, workflowPath);
        if (directResult.Workflow != null)
        {
            _logger.LogDebug("Workflow found via direct resolution: {WorkflowPath}", workflowPath);
            return directResult;
        }

        // Search all mount points for the workflow
        var searchResults = SearchAllMountPoints(config, workflowPath);
        if (searchResults.Any())
        {
            if (searchResults.Count > 1)
            {
                _logger.LogWarning("Multiple workflows found for {WorkflowPath}. Using first match. Found in: {Paths}",
                    workflowPath, string.Join(", ", searchResults.Select(r => r.LogicalPath)));
            }

            var result = searchResults.First();
            _logger.LogDebug("Workflow found at {LogicalPath}: {PhysicalPath}", result.LogicalPath, result.PhysicalPath);
            return (result.Workflow, result.LogicalPath);
        }

        _logger.LogError("Workflow not found: {WorkflowPath}", workflowPath);
        return (null, null);
    }

    public IEnumerable<string> ListWorkflows(DotRunConfigV2 config)
    {
        var workflows = new List<string>();

        foreach (var (logicalPath, physicalPath) in config.GetAllMountPoints())
        {
            if (!Directory.Exists(physicalPath))
            {
                _logger.LogDebug("Mount point does not exist: {PhysicalPath}", physicalPath);
                continue;
            }

            // Search for all .yaml files recursively
            var yamlFiles = Directory.GetFiles(physicalPath, "*.yaml", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(physicalPath, "*.yml", SearchOption.AllDirectories));

            foreach (var file in yamlFiles)
            {
                // Check if it's a valid workflow file
                if (IsWorkflowFile(file))
                {
                    var relativePath = Path.GetRelativePath(physicalPath, file);
                    var workflowName = Path.GetFileNameWithoutExtension(relativePath);
                    var workflowPath = BuildWorkflowPath(logicalPath, relativePath);
                    workflows.Add(workflowPath);
                }
            }
        }

        return workflows.Distinct().OrderBy(w => w);
    }

    public bool ValidateWorkflow(DotRunConfigV2 config, string workflowPath)
    {
        var result = GetByPath(config, workflowPath);
        return result.Workflow != null;
    }

    public IEnumerable<(string QualifiedPath, Workflow Workflow)> FindWorkflows(DotRunConfigV2 config, string workflowName)
    {
        var results = new List<(string QualifiedPath, Workflow Workflow)>();
        var searchResults = SearchAllMountPoints(config, workflowName);

        foreach (var result in searchResults)
        {
            results.Add((result.LogicalPath, result.Workflow));
        }

        return results;
    }

    private (Workflow Workflow, string QualifiedPath) TryDirectResolution(DotRunConfigV2 config, string workflowPath)
    {
        // Parse the workflow path
        var parts = workflowPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return (null, null);

        // Try to match against known mount points
        foreach (var repo in config.Repositories)
        {
            foreach (var path in repo.Paths)
            {
                var logicalPath = BuildLogicalPath(repo.Name, path.Mount);
                if (TryMatchPath(logicalPath, workflowPath, out var workflowFile))
                {
                    var physicalPath = ResolvePath(repo, path);
                    var fullPath = Path.Combine(physicalPath, workflowFile);

                    // Try with .yaml and .yml extensions
                    var workflow = TryLoadWorkflow(fullPath + ".yaml") ?? TryLoadWorkflow(fullPath + ".yml");
                    if (workflow != null)
                    {
                        // Build the fully qualified path
                        var qualifiedPath = string.IsNullOrEmpty(logicalPath)
                            ? workflowPath
                            : workflowPath; // Already qualified
                        return (workflow, qualifiedPath);
                    }
                }
            }
        }

        return (null, null);
    }

    private List<(string LogicalPath, string PhysicalPath, Workflow Workflow)> SearchAllMountPoints(
        DotRunConfigV2 config, string workflowName)
    {
        var results = new List<(string LogicalPath, string PhysicalPath, Workflow Workflow)>();

        foreach (var (logicalPath, physicalPath) in config.GetAllMountPoints())
        {
            if (!Directory.Exists(physicalPath))
                continue;

            // Build possible file paths
            var possiblePaths = BuildPossiblePaths(physicalPath, workflowName);

            foreach (var possiblePath in possiblePaths)
            {
                var workflow = TryLoadWorkflow(possiblePath);
                if (workflow != null)
                {
                    // Calculate the relative path from mount point to workflow
                    var relativePath = Path.GetRelativePath(physicalPath, possiblePath);
                    var workflowLogicalName = Path.GetFileNameWithoutExtension(relativePath);
                    var workflowDir = Path.GetDirectoryName(relativePath)?.Replace(Path.DirectorySeparatorChar, '/');

                    // Build the fully qualified logical path
                    var parts = new List<string>();
                    if (!string.IsNullOrEmpty(logicalPath))
                        parts.Add(logicalPath);
                    if (!string.IsNullOrEmpty(workflowDir) && workflowDir != ".")
                        parts.Add(workflowDir);
                    parts.Add(workflowLogicalName);

                    var fullLogicalPath = string.Join("/", parts);
                    results.Add((fullLogicalPath, possiblePath, workflow));
                }
            }
        }

        return results;
    }

    private List<string> BuildPossiblePaths(string basePath, string workflowName)
    {
        var paths = new List<string>();

        // Handle nested paths in workflow name (e.g., "subdir/workflow")
        var workflowPath = workflowName.Replace('/', Path.DirectorySeparatorChar);

        // Direct file in base path
        paths.Add(Path.Combine(basePath, workflowPath + ".yaml"));
        paths.Add(Path.Combine(basePath, workflowPath + ".yml"));

        // Search in subdirectories recursively
        if (!workflowName.Contains('/'))
        {
            // Only search subdirectories if workflow name doesn't already contain a path
            var yamlFiles = Directory.GetFiles(basePath, workflowName + ".yaml", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(basePath, workflowName + ".yml", SearchOption.AllDirectories));
            paths.AddRange(yamlFiles);
        }

        return paths;
    }

    private Workflow TryLoadWorkflow(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            // For now, create a temporary project to hold the workflow
            // This will be refactored when we fully migrate to the new system
            var workflowDir = Path.GetDirectoryName(filePath);
            var tempProject = Project.FromFile("", workflowDir);

            var workflow = Workflow.FromFile(tempProject, filePath);
            return workflow;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Failed to load workflow from {FilePath}: {Error}", filePath, ex.Message);
            return null;
        }
    }

    private bool IsWorkflowFile(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            return content.Contains("kind: workflow", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private string BuildLogicalPath(string repoName, string mount)
    {
        // Absolute mount - starts with /
        if (mount?.StartsWith("/") == true)
        {
            // Return without leading slash for consistency
            return mount.TrimStart('/');
        }

        // Relative mount under repository
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(repoName))
            parts.Add(repoName);
        if (!string.IsNullOrEmpty(mount))
            parts.Add(mount);

        return parts.Count > 0 ? string.Join("/", parts) : "";
    }

    private string BuildWorkflowPath(string logicalPath, string workflowFile)
    {
        var workflowName = Path.GetFileNameWithoutExtension(workflowFile);
        var workflowDir = Path.GetDirectoryName(workflowFile);

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(logicalPath))
            parts.Add(logicalPath);
        if (!string.IsNullOrEmpty(workflowDir) && workflowDir != ".")
            parts.Add(workflowDir.Replace(Path.DirectorySeparatorChar, '/'));
        parts.Add(workflowName);

        return string.Join("/", parts);
    }

    private bool TryMatchPath(string logicalPath, string workflowPath, out string workflowFile)
    {
        workflowFile = null;

        if (string.IsNullOrEmpty(logicalPath))
        {
            // Root mount - workflow path is the file
            workflowFile = workflowPath;
            return true;
        }

        if (workflowPath.StartsWith(logicalPath + "/"))
        {
            // Path matches - extract workflow file
            workflowFile = workflowPath.Substring(logicalPath.Length + 1);
            return true;
        }

        return false;
    }

    private string ResolvePath(Repository repo, RepositoryPath path)
    {
        if (IsAbsolutePath(path.Path))
        {
            return DirectoryHelper.GetAbsoluteLocalPath(path.Path);
        }
        else
        {
            return Path.Combine(repo.BasePath, path.Path);
        }
    }

    private bool IsAbsolutePath(string path)
    {
        return path.StartsWith("/") || path.StartsWith("~");
    }
}