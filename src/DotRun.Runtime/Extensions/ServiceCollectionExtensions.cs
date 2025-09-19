using DotRun.Runtime.Factories;
using DotRun.Runtime.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DotRun.Runtime.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDotRunServices(this IServiceCollection services)
    {
        // Services
        services.AddSingleton<IConfigurationLoader, ConfigurationLoader>();
        services.AddSingleton<IWorkflowExecutor, WorkflowExecutor>();
        services.AddSingleton<IProjectResolver, ProjectResolver>();
        
        // Factories
        services.AddSingleton<IShellFactory, ShellFactory>();
        services.AddSingleton<IStepExecutorFactory, StepExecutorFactory>();

        return services;
    }
}