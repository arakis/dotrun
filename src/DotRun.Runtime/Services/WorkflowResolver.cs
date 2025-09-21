using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotRun.Runtime.Model;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime.Services;

public class WorkflowResolver : IWorkflowResolver
{
    private readonly ILogger<WorkflowResolver> _logger;

    public WorkflowResolver(ILogger<WorkflowResolver> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IList<(string MountPath, WorkflowReference Reference)> ListWorkflowMountPaths(DotRunConfig config)
    {
        var results = new List<(string MountPath, WorkflowReference Reference)>();

        foreach (var (mountLogicalPath, mountPhysicalPath) in config.GetAllMountPoints())
        {
            if (!Directory.Exists(mountPhysicalPath))
            {
                _logger.LogDebug("Mount point does not exist: {PhysicalPath}", mountPhysicalPath);
                continue;
            }

            var workflowFiles = GetWorkflowFiles(mountPhysicalPath);

            foreach (var workflowFile in workflowFiles)
            {
                var relativePath = Path.GetRelativePath(mountPhysicalPath, workflowFile);
                var workflowName = Path.GetFileNameWithoutExtension(workflowFile);
                var workflowDir = Path.GetDirectoryName(relativePath)?.Replace(Path.DirectorySeparatorChar, '/');

                var relativeMountPath = string.IsNullOrEmpty(workflowDir) || workflowDir == "."
                    ? workflowName
                    : $"{workflowDir}/{workflowName}";

                var mountPath = string.IsNullOrEmpty(mountLogicalPath)
                    ? relativeMountPath
                    : $"{mountLogicalPath}/{relativeMountPath}";

                var reference = BuildWorkflowReferenceFromMount(config, mountLogicalPath, relativePath);
                if (reference != null) 
                    results.Add((mountPath, reference));
            }
        }

        return results;
    }

    /// <inheritdoc />
    public IList<WorkflowReference> ListWorkflowReferences(DotRunConfig config)
    {
        var references = new List<WorkflowReference>();

        foreach (var repository in config.Repositories)
        {
            if (!Directory.Exists(repository.BasePath))
            {
                _logger.LogDebug("Repository base path does not exist: {BasePath}", repository.BasePath);
                continue;
            }

            var workflowFiles = GetWorkflowFiles(repository.BasePath);

            foreach (var workflowFile in workflowFiles)
            {
                var relativePath = Path.GetRelativePath(repository.BasePath, workflowFile);
                var workflowName = Path.GetFileNameWithoutExtension(workflowFile);
                var workflowDir = Path.GetDirectoryName(relativePath)?.Replace(Path.DirectorySeparatorChar, '/');

                var reference = new WorkflowReference
                {
                    Repository = repository.Name,
                    Path = string.IsNullOrEmpty(workflowDir) || workflowDir == "." ? null : workflowDir,
                    Workflow = workflowName
                };

                references.Add(reference);
            }
        }

        return references.Distinct().ToList();
    }

    /// <inheritdoc />
    public (Workflow Workflow, WorkflowReference Reference) GetWorkflowByMountPath(DotRunConfig config, string mountPath)
    {
        if (string.IsNullOrWhiteSpace(mountPath))
        {
            _logger.LogError("Mount path is null or empty");
            return (null, null);
        }

        // Local function to build possible workflow file paths
        string[] BuildPossibleWorkflowPaths(string basePath, string workflowPath)
        {
            return new[]
            {
                Path.Combine(basePath, workflowPath + ".yaml"),
                Path.Combine(basePath, workflowPath + ".yml"),
                Path.Combine(basePath, workflowPath.Replace('/', Path.DirectorySeparatorChar) + ".yaml"),
                Path.Combine(basePath, workflowPath.Replace('/', Path.DirectorySeparatorChar) + ".yml")
            };
        }

        foreach (var (mountLogicalPath, mountPhysicalPath) in config.GetAllMountPoints())
        {
            if (!Directory.Exists(mountPhysicalPath))
                continue;

            // Check if the requested mount path is accessible via this mount
            // For root mount (""), any path is directly accessible
            // For non-root mounts, the path must start with the mount's logical path
            string relativeMountPath;
            if (!string.IsNullOrEmpty(mountLogicalPath))
            {
                if (!mountPath.StartsWith(mountLogicalPath + "/") && mountPath != mountLogicalPath)
                    continue;
                    
                // Strip the mount prefix to get the relative path within the mount
                relativeMountPath = mountPath == mountLogicalPath
                    ? ""
                    : mountPath.Substring(mountLogicalPath.Length + 1);
            }
            else
            {
                // Root mount - the mount path is used directly
                relativeMountPath = mountPath;
            }

            var possiblePaths = BuildPossibleWorkflowPaths(mountPhysicalPath, relativeMountPath);

            foreach (var possiblePath in possiblePaths)
            {
                var workflow = TryLoadWorkflow(possiblePath);
                if (workflow != null)
                {
                    var relativePath = Path.GetRelativePath(mountPhysicalPath, possiblePath);
                    var reference = BuildWorkflowReferenceFromMount(config, mountLogicalPath, relativePath);
                    
                    if (reference != null)
                    {
                        _logger.LogDebug("Found workflow at mount path {MountPath}: {Reference}", mountPath, reference);
                        return (workflow, reference);
                    }
                }
            }
        }

        _logger.LogError("Workflow not found at mount path: {MountPath}", mountPath);
        return (null, null);
    }

    /// <inheritdoc />
    public Workflow GetWorkflowByReference(DotRunConfig config, WorkflowReference reference)
    {
        var repository = config.Repositories.FirstOrDefault(r => r.Name == reference.Repository);
        if (repository == null)
        {
            _logger.LogError("Repository not found: {Repository}", reference.Repository);
            return null;
        }

        var pathParts = new List<string> { repository.BasePath };
        
        if (!string.IsNullOrEmpty(reference.Path))
            pathParts.Add(reference.Path);
        
        pathParts.Add(reference.Workflow);
        
        var basePath = Path.Combine(pathParts.ToArray());
        
        // Try both .yaml and .yml extensions
        foreach (var extension in new[] { ".yaml", ".yml" })
        {
            var possiblePath = basePath + extension;
            var workflow = TryLoadWorkflow(possiblePath);
            if (workflow != null)
            {
                _logger.LogDebug("Found workflow by reference {Reference} at {Path}", reference, possiblePath);
                return workflow;
            }
        }

        _logger.LogError("Workflow not found for reference: {Reference}", reference);
        return null;
    }

    private WorkflowReference BuildWorkflowReferenceFromMount(DotRunConfig config, string mountLogicalPath, string relativePath)
    {
        var workflowName = Path.GetFileNameWithoutExtension(relativePath);
        var workflowDir = Path.GetDirectoryName(relativePath)?.Replace(Path.DirectorySeparatorChar, '/');

        foreach (var repository in config.Repositories)
        {
            foreach (var path in repository.Paths)
            {
                var logicalPath = BuildLogicalPath(repository.Name, path.Mount);
                
                if (logicalPath == mountLogicalPath)
                {
                    var fullPath = string.IsNullOrEmpty(path.Path) || path.Path == "."
                        ? workflowDir
                        : string.IsNullOrEmpty(workflowDir) || workflowDir == "."
                            ? path.Path
                            : $"{path.Path}/{workflowDir}";

                    return new WorkflowReference
                    {
                        Repository = repository.Name,
                        Path = string.IsNullOrEmpty(fullPath) || fullPath == "." ? null : fullPath,
                        Workflow = workflowName
                    };
                }
            }
        }

        return null;
    }

    private string BuildLogicalPath(string repoName, string mount)
    {
        if (mount?.StartsWith("/") == true)
        {
            return mount.TrimStart('/');
        }

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(repoName))
            parts.Add(repoName);
        if (!string.IsNullOrEmpty(mount))
            parts.Add(mount);

        return parts.Count > 0 ? string.Join("/", parts) : "";
    }

    private IEnumerable<string> GetWorkflowFiles(string basePath)
    {
        if (!Directory.Exists(basePath))
            return Enumerable.Empty<string>();

        var yamlFiles = Directory.GetFiles(basePath, "*.yaml", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(basePath, "*.yml", SearchOption.AllDirectories));

        return yamlFiles.Where(IsWorkflowFile);
    }

    private bool IsWorkflowFile(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            return content.Contains("kind: workflow", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Failed to check if file is workflow {FilePath}: {Error}", filePath, ex.Message);
            return false;
        }
    }

    private Workflow TryLoadWorkflow(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
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
}