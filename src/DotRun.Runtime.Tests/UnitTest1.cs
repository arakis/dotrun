using System;
using System.IO;
using System.Threading.Tasks;
using DotRun.Database.Extensions;
using DotRun.Runtime.Extensions;
using DotRun.Runtime.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DotRun.Runtime.Tests;

public class UnitTest1 : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IWorkflowExecutor _workflowExecutor;
    private readonly string _configFile;

    public UnitTest1()
    {
        // Setup DI container
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        // Add DotRun services
        services.AddDotRunServices();
        
        // Add DotRun database services
        services.AddDotRunDatabase();
        
        // Build service provider
        _serviceProvider = services.BuildServiceProvider();
        _workflowExecutor = _serviceProvider.GetRequiredService<IWorkflowExecutor>();
        
        // Get config file path
        var binDir = Path.GetDirectoryName(new FileInfo(typeof(UnitTest1).Assembly.Location).FullName);
        _configFile = Path.Combine(binDir, "Configuration", "config.yaml");
    }

    private async Task<WorkflowResult> Run(string project, string workflow)
    {
        // Load configuration
        var configLoader = _serviceProvider.GetRequiredService<IConfigurationLoader>();
        var config = await configLoader.LoadAsync(_configFile);
        
        // Execute workflow
        return await _workflowExecutor.ExecuteAsync(config, project, workflow);
    }

    public void Dispose()
        => _serviceProvider?.Dispose();

    [Fact]
    [Trait("Node", "Local")]
    public async Task Local()
    {
        var result = await Run("proj1", "local");
        Assert.False(result.Failed);
    }

    [Fact]
    [Trait("Node", "Docker")]
    public async Task Docker()
    {
        var result = await Run("proj1", "docker");
        Assert.False(result.Failed);
    }

    [Fact]
    [Trait("Node", "Local")]
    public async Task Error()
    {
        var result = await Run("proj1", "error");
        Assert.True(result.Failed);
    }
}