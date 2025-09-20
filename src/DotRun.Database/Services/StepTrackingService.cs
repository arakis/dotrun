using Microsoft.Extensions.Logging;
using DotRun.Runtime.Services.Tracking;
using DotRun.Runtime.Model;

namespace DotRun.Database.Services;

public class StepTrackingService : IStepTrackingService
{
    private readonly DotRunDbContext _context;
    private readonly ILogger<StepTrackingService> _logger;
    
    public StepTrackingService(DotRunDbContext context, ILogger<StepTrackingService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task CreateStepRunAsync(Guid stepRunId, Guid jobRunId, string name)
    {
        var stepRun = new StepRunEntity
        {
            Id = stepRunId,
            JobRunId = jobRunId,
            Name = name ?? "Unnamed Step",
            Slug = name ?? "unnamed-step",
            Status = ExecutionStatus.Queued
        };
        
        _context.StepRun.Add(stepRun);
        await _context.SaveChangesAsync();
    }
    
    public async Task StartStepRunAsync(Guid stepRunId)
    {
        var stepRun = await _context.StepRun.FindAsync(stepRunId);
        if (stepRun != null)
        {
            stepRun.Status = ExecutionStatus.Started;
            stepRun.StartedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task CompleteStepRunAsync(Guid stepRunId, ExecutionStatus status)
    {
        var stepRun = await _context.StepRun.FindAsync(stepRunId);
        if (stepRun != null)
        {
            stepRun.Status = status;
            stepRun.FinishedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task SkipStepRunAsync(Guid stepRunId)
    {
        var stepRun = await _context.StepRun.FindAsync(stepRunId);
        if (stepRun != null)
        {
            stepRun.Status = ExecutionStatus.Skipped;
            stepRun.StartedAt = DateTimeOffset.UtcNow;
            stepRun.FinishedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}