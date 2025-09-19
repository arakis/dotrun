using DotRun.Runtime.Factories;
using DotRun.Runtime.Runners;
using DotRun.Runtime.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DotRun.Runtime.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDotRunServices(this IServiceCollection services)
    {
        // Core Services
        services.AddSingleton<IConfigurationLoader, ConfigurationLoader>();
        services.AddSingleton<IWorkflowExecutor, WorkflowExecutor>();
        services.AddSingleton<IProjectResolver, ProjectResolver>();
        
        // Runners (with constructor DI)
        services.AddSingleton<IWorkflowRunner, WorkflowRunner>();
        services.AddSingleton<IJobRunner, JobRunner>();
        services.AddSingleton<IStepRunner, StepRunner>();
        
        // Factories
        services.AddSingleton<INodeFactory, NodeFactory>();
        services.AddSingleton<IShellFactory, ShellFactory>();
        services.AddSingleton<IStepExecutorFactory, StepExecutorFactory>();

        return services;
    }
}