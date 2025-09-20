using DotRun.Database.Services;
using DotRun.Runtime.Services.Tracking;
using DotRun.Runtime.Services.V2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DotRun.Database.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers DotRun database services and tracking services with their appropriate lifetimes.
    /// </summary>
    public static IServiceCollection AddDotRunDatabase(this IServiceCollection services)
    {
        // Database Context - Scoped
        // Each workflow run gets its own DbContext for isolation
        services.AddDbContext<DotRunDbContext>(ServiceLifetime.Scoped);
        
        // Tracking Services - Scoped
        // Services that persist workflow execution state to database
        services.AddScoped<IWorkflowTrackingService, WorkflowTrackingService>();
        services.AddScoped<IJobTrackingService, JobTrackingService>();
        services.AddScoped<IStepTrackingService, StepTrackingService>();
        
        // V2 Services - Scoped
        // New mount-based workflow resolution and syncing
        services.AddScoped<IWorkflowResolver, WorkflowResolver>();
        services.AddScoped<IWorkflowSyncService, WorkflowSyncService>();

        return services;
    }
}