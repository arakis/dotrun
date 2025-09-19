using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime.Configuration;

public class DotRunOptions
{
    public const string ConfigurationSection = "DotRun";

    [Required(ErrorMessage = "Configuration file path is required")]
    public string ConfigFile { get; set; }

    [Required(ErrorMessage = "Project name is required")]
    public string Project { get; set; }

    [Required(ErrorMessage = "Workflow name is required")]
    public string Workflow { get; set; }

    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    public bool DryRun { get; set; }

    public int TimeoutSeconds { get; set; } = 3600; // Default 1 hour

    public bool EnableTelemetry { get; set; }

    public string WorkingDirectory { get; set; }

    public string[] AdditionalArgs { get; set; } = [];
}