using DotRun.Runtime.Factories;
using DotRun.Runtime.Runners;
using DotRun.Runtime.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DotRun.Runtime.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all DotRun runtime services with their appropriate lifetimes.
    /// </summary>
    public static IServiceCollection AddDotRunServices(this IServiceCollection services)
    {
        // Core Services - Singleton
        // These services maintain configuration state and should be shared across the application
        services.AddSingleton<IConfigurationLoader, ConfigurationLoader>();
        services.AddSingleton<IProjectResolver, ProjectResolver>();

        // Workflow Executor - Singleton
        // Stateless orchestrator that creates scoped contexts for each execution
        services.AddSingleton<IWorkflowExecutor, WorkflowExecutor>();

        // Runners - Scoped
        // Services that execute workflows, jobs, and steps with tracking
        // Each workflow run gets its own scoped instances with isolated DbContext
        services.AddScoped<IWorkflowRunner, WorkflowRunner>();
        services.AddScoped<IJobRunner, JobRunner>();
        services.AddScoped<IStepRunner, StepRunner>();

        // Factories - Singleton
        // Stateless factories that create instances on demand
        // Created objects (nodes, shells, executors) have their own lifecycle
        services.AddSingleton<INodeFactory, NodeFactory>();
        services.AddSingleton<IShellFactory, ShellFactory>();
        services.AddSingleton<IStepExecutorFactory, StepExecutorFactory>();

        return services;
    }
}