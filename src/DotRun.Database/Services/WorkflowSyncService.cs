using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DotRun.Runtime;
using DotRun.Runtime.Services.Tracking;
using DotRun.Runtime.Model.V2;
using DotRun.Runtime.Services.V2;

namespace DotRun.Database.Services;

public class WorkflowSyncService : IWorkflowSyncService
{
    private readonly DotRunDbContext _context;
    private readonly ILogger<WorkflowSyncService> _logger;
    private readonly IWorkflowResolver _workflowResolver;

    public WorkflowSyncService(
        DotRunDbContext context, 
        ILogger<WorkflowSyncService> logger,
        IWorkflowResolver workflowResolver)
    {
        _context = context;
        _logger = logger;
        _workflowResolver = workflowResolver;
    }

    public async Task<WorkflowSyncResult> SyncWorkflowsAsync(DotRunConfigV2 config)
    {
        var result = new WorkflowSyncResult();
        var discoveredPaths = new HashSet<string>();

        try
        {
            // Discover all workflows from mounted paths
            var mountPaths = _workflowResolver.ListWorkflowMountPaths(config);
            result.DiscoveredCount = mountPaths.Count;
            
            _logger.LogInformation("Discovered {Count} workflows", mountPaths.Count);

            // Process each discovered workflow
            foreach (var (mountPath, reference) in mountPaths)
            {
                var qualifiedPath = reference.ToString();
                discoveredPaths.Add(qualifiedPath);
                
                try
                {
                    var workflow = _workflowResolver.GetWorkflowByReference(config, reference);
                    if (workflow == null)
                    {
                        _logger.LogWarning("Could not resolve workflow for path: {Path}", qualifiedPath);
                        result.Errors.Add($"Failed to resolve: {qualifiedPath}");
                        continue;
                    }

                    // Read YAML content and calculate hash
                    var yamlContent = File.ReadAllText(workflow.WorkflowFilePath);
                    var hash = ComputeHash(yamlContent);

                    // Check if workflow exists in database
                    var entity = await _context.Workflow
                        .FirstOrDefaultAsync(w => w.QualifiedPath == qualifiedPath);

                    if (entity == null)
                    {
                        // Create new workflow entity
                        entity = new WorkflowEntity
                        {
                            QualifiedPath = qualifiedPath,
                            Name = workflow.Name ?? Path.GetFileNameWithoutExtension(qualifiedPath),
                            Slug = GenerateSlug(qualifiedPath),
                            YamlFilePath = workflow.WorkflowFilePath,
                            SourceHash = hash,
                            DeactivatedAt = null
                        };
                        _context.Workflow.Add(entity);
                        result.CreatedCount++;
                        _logger.LogInformation("Created workflow: {Path}", qualifiedPath);
                    }
                    else
                    {
                        // Reactivate if was deactivated
                        if (entity.DeactivatedAt.HasValue)
                        {
                            _logger.LogInformation("Reactivating workflow: {Path}", qualifiedPath);
                            entity.DeactivatedAt = null;
                            result.ReactivatedCount++;
                        }

                        // Update existing workflow
                        var changed = false;
                        if (entity.YamlFilePath != workflow.WorkflowFilePath)
                        {
                            entity.YamlFilePath = workflow.WorkflowFilePath;
                            changed = true;
                        }
                        if (entity.SourceHash != hash)
                        {
                            entity.SourceHash = hash;
                            changed = true;
                        }
                        if (entity.Name != workflow.Name && !string.IsNullOrEmpty(workflow.Name))
                        {
                            entity.Name = workflow.Name;
                            changed = true;
                        }
                        
                        if (changed)
                        {
                            result.UpdatedCount++;
                            _logger.LogDebug("Updated workflow: {Path}", qualifiedPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing workflow: {Path}", qualifiedPath);
                    result.Errors.Add($"Error processing {qualifiedPath}: {ex.Message}");
                }
            }

            // Save all creates/updates
            await _context.SaveChangesAsync();

            // Deactivate missing workflows
            var now = DateTimeOffset.UtcNow;
            var toDeactivate = await _context.Workflow
                .Where(w => w.DeactivatedAt == null && !discoveredPaths.Contains(w.QualifiedPath))
                .ToListAsync();

            foreach (var workflow in toDeactivate)
            {
                workflow.DeactivatedAt = now;
                result.DeactivatedCount++;
                _logger.LogInformation("Deactivated workflow: {Path}", workflow.QualifiedPath);
            }

            if (toDeactivate.Any()) 
                await _context.SaveChangesAsync();

            _logger.LogInformation("Sync completed: Created={Created}, Updated={Updated}, Reactivated={Reactivated}, Deactivated={Deactivated}",
                result.CreatedCount, result.UpdatedCount, result.ReactivatedCount, result.DeactivatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during workflow sync");
            result.Errors.Add($"Sync failed: {ex.Message}");
        }

        return result;
    }

    public async Task<WorkflowSyncResult> SyncSingleWorkflowAsync(DotRunConfigV2 config, string qualifiedPath)
    {
        var result = new WorkflowSyncResult();

        try
        {
            // Parse the qualified path to get a reference
            var reference = WorkflowReference.Parse(qualifiedPath);
            var workflow = _workflowResolver.GetWorkflowByReference(config, reference);
            if (workflow == null)
            {
                // Mark as deactivated if it exists in database
                var entity = await _context.Workflow
                    .FirstOrDefaultAsync(w => w.QualifiedPath == qualifiedPath);
                    
                if (entity != null && entity.DeactivatedAt == null)
                {
                    entity.DeactivatedAt = DateTimeOffset.UtcNow;
                    await _context.SaveChangesAsync();
                    result.DeactivatedCount = 1;
                    _logger.LogInformation("Deactivated workflow: {Path}", qualifiedPath);
                }
                return result;
            }

            // Process the single workflow (same logic as in bulk sync)
            var yamlContent = File.ReadAllText(workflow.WorkflowFilePath);
            var hash = ComputeHash(yamlContent);

            var existingEntity = await _context.Workflow
                .FirstOrDefaultAsync(w => w.QualifiedPath == qualifiedPath);

            if (existingEntity == null)
            {
                var newEntity = new WorkflowEntity
                {
                    QualifiedPath = qualifiedPath,
                    Name = workflow.Name ?? Path.GetFileNameWithoutExtension(qualifiedPath),
                    Slug = GenerateSlug(qualifiedPath),
                    YamlFilePath = workflow.WorkflowFilePath,
                    SourceHash = hash,
                    DeactivatedAt = null
                };
                _context.Workflow.Add(newEntity);
                result.CreatedCount = 1;
            }
            else
            {
                if (existingEntity.DeactivatedAt.HasValue)
                {
                    existingEntity.DeactivatedAt = null;
                    result.ReactivatedCount = 1;
                }
                
                existingEntity.YamlFilePath = workflow.WorkflowFilePath;
                existingEntity.SourceHash = hash;
                if (!string.IsNullOrEmpty(workflow.Name))
                    existingEntity.Name = workflow.Name;
                    
                result.UpdatedCount = 1;
            }

            await _context.SaveChangesAsync();
            result.DiscoveredCount = 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing single workflow: {Path}", qualifiedPath);
            result.Errors.Add($"Sync failed for {qualifiedPath}: {ex.Message}");
        }

        return result;
    }

    private string ComputeHash(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private string GenerateSlug(string qualifiedPath)
    {
        // Convert path to slug: repo/path/workflow -> repo-path-workflow
        return qualifiedPath.Replace('/', '-').ToLowerInvariant();
    }
}