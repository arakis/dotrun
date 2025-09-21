using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using DotRun.Console.Commands;
using DotRun.Database.Extensions;
using DotRun.Database.Services;
using DotRun.Runtime.Configuration;
using DotRun.Runtime.Extensions;
using DotRun.Runtime.Model;
using DotRun.Runtime.Services.Tracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotRun.Console;

class Program
{
    static async Task<int> Main(string[] args)
    {
        return await Parser.Default.ParseArguments<RunCommand, SyncCommand>(args)
            .MapResult(
                async (RunCommand runCmd) => await RunHostAsync(runCmd, args),
                async (SyncCommand syncCmd) => await RunSyncAsync(syncCmd),
                errors => Task.FromResult((int)ExitCode.InvalidArguments));
    }

    private static async Task<int> RunSyncAsync(SyncCommand options)
    {
        try
        {
            // Simple host for sync operation
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddConsole();
                        logging.SetMinimumLevel(options.LogLevel);
                    });
                    
                    services.AddDotRunDatabase();
                })
                .Build();
                
            using (var scope = host.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                var syncService = scope.ServiceProvider.GetRequiredService<IWorkflowSyncService>();
                
                // Load V2 config
                var configPath = Path.GetFullPath(options.ConfigFile);
                if (!File.Exists(configPath))
                {
                    logger.LogError("Configuration file not found: {Path}", configPath);
                    return (int)ExitCode.ConfigurationError;
                }
                
                var config = DotRunConfigV2.FromFile(configPath);
                logger.LogInformation("Starting workflow sync from: {Path}", configPath);
                
                var result = await syncService.SyncWorkflowsAsync(config);
                
                logger.LogInformation("Sync completed: Created={Created}, Updated={Updated}, Reactivated={Reactivated}, Deactivated={Deactivated}",
                    result.CreatedCount, result.UpdatedCount, result.ReactivatedCount, result.DeactivatedCount);
                    
                if (result.Errors.Count > 0)
                {
                    logger.LogWarning("Sync completed with {Count} errors", result.Errors.Count);
                    foreach (var error in result.Errors)
                    {
                        logger.LogError("Sync error: {Error}", error);
                    }
                }
                
                return 0;
            }
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine($"Sync failed: {ex.Message}");
            return (int)ExitCode.UnhandledException;
        }
    }

    private static async Task<int> RunHostAsync(RunCommand options, string[] args)
    {
        try
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                    config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json",
                        optional: true, reloadOnChange: false);
                    config.AddEnvironmentVariables(prefix: "DOTRUN_");
                    config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Configure options
                    services.Configure<DotRunOptions>(dotRunOptions =>
                    {
                        dotRunOptions.ConfigFile = options.ConfigFile;
                        dotRunOptions.Project = options.Project;
                        dotRunOptions.Workflow = options.Workflow;
                        dotRunOptions.LogLevel = options.LogLevel;
                        dotRunOptions.TimeoutSeconds = options.Timeout;
                        dotRunOptions.DryRun = options.DryRun;
                        dotRunOptions.WorkingDirectory = options.WorkingDirectory;
                    });

                    // Add DotRun services
                    services.AddDotRunServices();

                    // Add DotRun database services
                    services.AddDotRunDatabase();

                    // Add hosted service
                    services.AddHostedService<DotRunHostedService>();
                })
                .ConfigureLogging((hostContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole(loggerOptions => { loggerOptions.FormatterName = nameof(ColoredConsoleFormatter); })
                        .AddConsoleFormatter<ColoredConsoleFormatter, ColoredConsoleFormatterOptions>(formatterOptions =>
                        {
                            formatterOptions.TimestampFormat = "[HH:mm:ss.fff] ";
                            formatterOptions.UseUtcTimestamp = false; // Use local time
                        });
                    logging.SetMinimumLevel(options.LogLevel);
                })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
            return Environment.ExitCode;
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine($"Fatal error: {ex.Message}");
            return (int)ExitCode.UnhandledException;
        }
    }
}