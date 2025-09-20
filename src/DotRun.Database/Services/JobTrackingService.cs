using Microsoft.Extensions.Logging;
using DotRun.Runtime.Services.Tracking;
using DotRun.Runtime.Model;

namespace DotRun.Database.Services;

public class JobTrackingService : IJobTrackingService
{
    private readonly DotRunDbContext _context;
    private readonly ILogger<JobTrackingService> _logger;
    
    public JobTrackingService(DotRunDbContext context, ILogger<JobTrackingService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task CreateJobRunAsync(Guid jobRunId, Guid workflowRunId, Guid workflowId, string name)
    {
        var jobRun = new JobRunEntity
        {
            Id = jobRunId,
            WorkflowRunId = workflowRunId,
            WorkflowId = workflowId,
            Name = name ?? "Unnamed Job",
            Slug = name ?? "unnamed-job",
            Status = ExecutionStatus.Queued
        };
        
        _context.JobRun.Add(jobRun);
        await _context.SaveChangesAsync();
    }
    
    public async Task StartJobRunAsync(Guid jobRunId)
    {
        var jobRun = await _context.JobRun.FindAsync(jobRunId);
        if (jobRun != null)
        {
            jobRun.Status = ExecutionStatus.Started;
            jobRun.StartedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task CompleteJobRunAsync(Guid jobRunId, ExecutionStatus status)
    {
        var jobRun = await _context.JobRun.FindAsync(jobRunId);
        if (jobRun != null)
        {
            jobRun.Status = status;
            jobRun.FinishedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task SkipJobRunAsync(Guid jobRunId)
    {
        var jobRun = await _context.JobRun.FindAsync(jobRunId);
        if (jobRun != null)
        {
            jobRun.Status = ExecutionStatus.Skipped;
            jobRun.StartedAt = DateTimeOffset.UtcNow;
            jobRun.FinishedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}