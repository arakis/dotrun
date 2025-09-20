using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DotRun.Runtime;
using DotRun.Runtime.Services.Tracking;
using DotRun.Runtime.Model;

namespace DotRun.Database.Services;

public class WorkflowTrackingService : IWorkflowTrackingService
{
    private readonly DotRunDbContext _context;
    private readonly ILogger<WorkflowTrackingService> _logger;
    
    public WorkflowTrackingService(DotRunDbContext context, ILogger<WorkflowTrackingService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    // Legacy method for backward compatibility
    public async Task<Guid> EnsureWorkflowEntityAsync(string projectName, string workflowName)
    {
        var qualifiedPath = $"{projectName}/{workflowName}";
        return await EnsureWorkflowByPathAsync(qualifiedPath);
    }
    
    // New path-based method
    public async Task<Guid> EnsureWorkflowByPathAsync(string qualifiedPath)
    {
        var workflow = await _context.Workflow
            .FirstOrDefaultAsync(w => w.QualifiedPath == qualifiedPath && w.DeactivatedAt == null);
            
        if (workflow == null)
        {
            // Extract name from path
            var name = Path.GetFileNameWithoutExtension(qualifiedPath);
            
            workflow = new WorkflowEntity 
            {
                Name = name,
                Slug = qualifiedPath.Replace('/', '-').ToLowerInvariant(),
                QualifiedPath = qualifiedPath,
                DeactivatedAt = null,
                YamlFilePath = "", // Will be updated by sync service
                SourceHash = "" // Will be updated by sync service
            };
            _context.Workflow.Add(workflow);
        }
        
        workflow.LastRunAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
        
        return workflow.Id;
    }
    
    public async Task<Guid> CreateWorkflowRunAsync(Guid workflowId, string name, string slug, string source)
    {
        var workflowRun = new WorkflowRunEntity
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            Name = name ?? "Unnamed Run",
            Slug = slug ?? "unnamed-run",
            Source = source,
            Status = ExecutionStatus.Queued
        };
        
        _context.WorkflowRun.Add(workflowRun);
        await _context.SaveChangesAsync();
        
        return workflowRun.Id;
    }
    
    public async Task CreateExecutionPlanAsync(Guid workflowRunId, Guid workflowId, WorkflowContext workflowContext)
    {
        foreach (var jobContext in workflowContext.JobContexts)
        {
            var jobRun = new JobRunEntity
            {
                Id = jobContext.JobRunId,
                WorkflowRunId = workflowRunId,
                WorkflowId = workflowId,
                Name = jobContext.Job.Name ?? "Unnamed Job",
                Slug = jobContext.Job.Name ?? "unnamed-job",
                Status = ExecutionStatus.Queued
            };
            _context.JobRun.Add(jobRun);
            
            if (jobContext.StepContexts != null)
            {
                foreach (var stepContext in jobContext.StepContexts)
                {
                    var stepRun = new StepRunEntity
                    {
                        Id = stepContext.StepRunId,
                        JobRunId = jobContext.JobRunId,
                        Name = stepContext.Step.Name ?? "Unnamed Step",
                        Slug = stepContext.Step.Name ?? "unnamed-step",
                        Status = ExecutionStatus.Queued
                    };
                    _context.StepRun.Add(stepRun);
                }
            }
            
            if (jobContext.SubJobContexts != null)
            {
                await CreateSubJobExecutionPlanAsync(workflowRunId, workflowId, jobContext);
            }
        }
        
        await _context.SaveChangesAsync();
    }
    
    private async Task CreateSubJobExecutionPlanAsync(Guid workflowRunId, Guid workflowId, JobContext parentJobContext)
    {
        foreach (var subJobContext in parentJobContext.SubJobContexts)
        {
            var jobRun = new JobRunEntity
            {
                Id = subJobContext.JobRunId,
                WorkflowRunId = workflowRunId,
                WorkflowId = workflowId,
                Name = subJobContext.Job.Name ?? "Unnamed Job",
                Slug = subJobContext.Job.Name ?? "unnamed-job",
                Status = ExecutionStatus.Queued
            };
            _context.JobRun.Add(jobRun);
            
            if (subJobContext.StepContexts != null)
            {
                foreach (var stepContext in subJobContext.StepContexts)
                {
                    var stepRun = new StepRunEntity
                    {
                        Id = stepContext.StepRunId,
                        JobRunId = subJobContext.JobRunId,
                        Name = stepContext.Step.Name ?? "Unnamed Step",
                        Slug = stepContext.Step.Name ?? "unnamed-step",
                        Status = ExecutionStatus.Queued
                    };
                    _context.StepRun.Add(stepRun);
                }
            }
            
            if (subJobContext.SubJobContexts != null)
            {
                await CreateSubJobExecutionPlanAsync(workflowRunId, workflowId, subJobContext);
            }
        }
    }
    
    public async Task StartWorkflowRunAsync(Guid runId)
    {
        var run = await _context.WorkflowRun.FindAsync(runId);
        if (run != null)
        {
            run.Status = ExecutionStatus.Started;
            run.StartedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task CompleteWorkflowRunAsync(Guid runId, ExecutionStatus status)
    {
        var run = await _context.WorkflowRun.FindAsync(runId);
        if (run != null)
        {
            run.Status = status;
            run.FinishedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}