using CommandLine;
using Microsoft.Extensions.Logging;

namespace DotRun.Console;

public class CommandLineOptions
{
    [Option('c', "config-file", Required = true, HelpText = "Path to the configuration file")]
    public string ConfigFile { get; set; }

    [Option('p', "project", Required = true, HelpText = "Name of the project to run")]
    public string Project { get; set; }

    [Option('w', "workflow", Required = true, HelpText = "Name of the workflow to execute")]
    public string Workflow { get; set; }

    [Option('l', "log-level", Default = LogLevel.Information, HelpText = "Set the logging level")]
    public LogLevel LogLevel { get; set; }

    [Option('t', "timeout", Default = 3600, HelpText = "Timeout in seconds")]
    public int Timeout { get; set; }

    [Option('d', "dry-run", Default = false, HelpText = "Perform a dry run without executing actions")]
    public bool DryRun { get; set; }

    [Option("working-directory", HelpText = "Set the working directory")]
    public string WorkingDirectory { get; set; }
}