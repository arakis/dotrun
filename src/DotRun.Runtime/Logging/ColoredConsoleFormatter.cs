using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace DotRun;

public sealed class ColoredConsoleFormatter : ConsoleFormatter
{
    private readonly ColoredConsoleFormatterOptions _options;

    // ANSI color codes
    private const string Reset = "\u001b[0m";
    private const string DarkGray = "\u001b[90m";
    private const string Red = "\u001b[31m";
    private const string Green = "\u001b[32m";
    private const string Yellow = "\u001b[33m";
    private const string Blue = "\u001b[34m";
    private const string Magenta = "\u001b[35m";
    private const string Cyan = "\u001b[36m";
    private const string White = "\u001b[37m";
    private const string BrightRed = "\u001b[91m";
    private const string BrightGreen = "\u001b[92m";
    private const string Gray = "\u001b[37m";

    public ColoredConsoleFormatter(IOptionsMonitor<ColoredConsoleFormatterOptions> options)
        : base(nameof(ColoredConsoleFormatter))
    {
        _options = options.CurrentValue;
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        string message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (message is null)
            return;

        // Write timestamp
        if (!string.IsNullOrEmpty(_options.TimestampFormat))
        {
            var timestamp = _options.UseUtcTimestamp
                ? DateTimeOffset.UtcNow.ToString(_options.TimestampFormat)
                : DateTimeOffset.Now.ToString(_options.TimestampFormat);

            textWriter.Write($"{DarkGray}{timestamp}{Reset}");
        }

        // Write log level
        var levelColor = GetLogLevelAnsiColor(logEntry.LogLevel);
        textWriter.Write($"{levelColor}{GetLogLevelString(logEntry.LogLevel)}: {Reset}");

        // Write category (scope) in cyan
        textWriter.Write($"{Cyan}{logEntry.Category}[{logEntry.EventId.Id}]{Reset}");

        // Write the actual message
        textWriter.WriteLine($" {message}");

        // Write exception if exists
        if (logEntry.Exception != null)
        {
            textWriter.WriteLine($"{Red}{logEntry.Exception}{Reset}");
        }
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };
    }

    private static string GetLogLevelAnsiColor(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => Gray,
            LogLevel.Debug => Gray,
            LogLevel.Information => Green,
            LogLevel.Warning => Yellow,
            LogLevel.Error => BrightRed,
            LogLevel.Critical => Red,
            _ => Reset
        };
    }
}

public sealed class ColoredConsoleFormatterOptions : ConsoleFormatterOptions
{
}
