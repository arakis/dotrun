using System;
using System.Threading.Tasks;
using CommandLine;
using DotRun.Database.Extensions;
using DotRun.Runtime.Configuration;
using DotRun.Runtime.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotRun.Console;

class Program
{
    static async Task<int> Main(string[] args)
    {
        return await Parser.Default.ParseArguments<CommandLineOptions>(args)
            .MapResult(
                async options => await RunHostAsync(options, args),
                errors => Task.FromResult((int)ExitCode.InvalidArguments));
    }

    private static async Task<int> RunHostAsync(CommandLineOptions options, string[] args)
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