using System;
using System.Threading;
using System.Threading.Tasks;
using DotRun.Runtime;
using DotRun.Runtime.Configuration;
using DotRun.Runtime.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotRun.Console;

public class DotRunHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<DotRunHostedService> _logger;
    private readonly IOptions<DotRunOptions> _options;
    private readonly IConfigurationLoader _configLoader;
    private readonly IWorkflowExecutor _workflowExecutor;
    
    private int _exitCode = (int)ExitCode.Success;

    public DotRunHostedService(
        IHostApplicationLifetime appLifetime,
        ILogger<DotRunHostedService> logger,
        IOptions<DotRunOptions> options,
        IConfigurationLoader configLoader,
        IWorkflowExecutor workflowExecutor)
    {
        _appLifetime = appLifetime;
        _logger = logger;
        _options = options;
        _configLoader = configLoader;
        _workflowExecutor = workflowExecutor;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting DotRun hosted service");
        
        // Register to handle the application stopping gracefully
        _appLifetime.ApplicationStarted.Register(() =>
        {
            Task.Run(async () =>
            {
                try
                {
                    await RunAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Application execution cancelled");
                    _exitCode = (int)ExitCode.Cancelled;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Unhandled exception in hosted service");
                    _exitCode = (int)ExitCode.UnhandledException;
                }
                finally
                {
                    // Stop the application once the work is done
                    _appLifetime.StopApplication();
                }
            }, cancellationToken);
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Stopping DotRun hosted service with exit code: {ExitCode}", _exitCode);
        
        // Set the exit code for the application
        Environment.ExitCode = _exitCode;
        
        return Task.CompletedTask;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        
        _logger.LogInformation("DotRun starting");
        _logger.LogInformation("Configuration file: {ConfigFile}", options.ConfigFile);
        _logger.LogInformation("Project: {Project}", options.Project);
        _logger.LogInformation("Workflow: {Workflow}", options.Workflow);

        try
        {
            // Load configuration
            _logger.LogDebug("Loading configuration");
            var config = await _configLoader.LoadAsync(options.ConfigFile);
            
            // Validate configuration
            if (!await _configLoader.ValidateAsync(config))
            {
                _logger.LogError("Configuration validation failed");
                _exitCode = (int)ExitCode.ConfigurationError;
                return;
            }

            // Execute workflow with timeout support
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            if (options.TimeoutSeconds > 0)
            {
                cts.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
            }

            try
            {
                var result = await _workflowExecutor.ExecuteAsync(
                    config,
                    options.Project,
                    options.Workflow,
                    cts.Token);

                if (result.Failed)
                {
                    _logger.LogError("Workflow execution failed");
                    _exitCode = (int)ExitCode.WorkflowExecutionFailed;
                }
                else
                {
                    _logger.LogInformation("Workflow execution completed successfully");
                    _exitCode = (int)ExitCode.Success;
                }
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogError("Workflow execution timed out after {Timeout} seconds", options.TimeoutSeconds);
                _exitCode = (int)ExitCode.Timeout;
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Project") && ex.Message.Contains("not found"))
        {
            _logger.LogError(ex, "Project not found");
            _exitCode = (int)ExitCode.ProjectNotFound;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Workflow") && ex.Message.Contains("not found"))
        {
            _logger.LogError(ex, "Workflow not found");
            _exitCode = (int)ExitCode.WorkflowNotFound;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow execution failed with error");
            _exitCode = (int)ExitCode.GeneralError;
        }
    }
}