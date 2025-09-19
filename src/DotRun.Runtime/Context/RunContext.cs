using System;
using Microsoft.Extensions.DependencyInjection;

namespace DotRun.Runtime;

public class RunContext : IDisposable
{
    private readonly IServiceScope _serviceScope;
    
    /// <summary>
    /// Gets the scoped service provider for this execution context.
    /// </summary>
    public IServiceProvider ServiceProvider => _serviceScope.ServiceProvider;
    
    /// <summary>
    /// Gets the service scope for manual management if needed.
    /// </summary>
    internal IServiceScope ServiceScope => _serviceScope;

    /// <summary>
    /// Creates a RunContext with its own service scope.
    /// </summary>
    public RunContext(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
            
        _serviceScope = serviceProvider.CreateScope();
    }

    public void Dispose()
    {
        _serviceScope?.Dispose();
    }
}