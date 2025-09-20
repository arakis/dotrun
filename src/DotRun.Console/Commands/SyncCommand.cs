using CommandLine;
using Microsoft.Extensions.Logging;

namespace DotRun.Console.Commands;

[Verb("sync", HelpText = "Synchronize workflows with the database")]
public class SyncCommand
{
    [Option('c', "config-file", Required = true, HelpText = "Path to the V2 configuration file")]
    public string ConfigFile { get; set; }

    [Option('l', "log-level", Default = LogLevel.Information, HelpText = "Set the logging level")]
    public LogLevel LogLevel { get; set; }
}