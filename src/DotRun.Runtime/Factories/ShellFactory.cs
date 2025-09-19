using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime.Factories;

public class ShellFactory : IShellFactory
{
    private readonly ILogger<ShellFactory> _logger;

    public ShellFactory(ILogger<ShellFactory> logger)
    {
        _logger = logger;
    }

    public IShell CreateShell(string shellType)
    {
        if (string.IsNullOrWhiteSpace(shellType))
        {
            _logger.LogDebug("Shell type not specified, using default shell");
            return GetDefaultShell();
        }

        _logger.LogDebug("Creating shell of type: {ShellType}", shellType);

        IShell shell = shellType.ToLowerInvariant() switch
        {
            "sh" => new ShShell(),
            "bash" => new ShShell(),
            "powershell" => new PowerShell(),
            "pwsh" => new PowerShell(),
            "cmd" => new CmdShell(),
            _ => throw new NotSupportedException($"Shell type '{shellType}' is not supported")
        };

        _logger.LogInformation("Created {ShellType} shell", shellType);
        return shell;
    }

    public IShell GetDefaultShell()
    {
        IShell shell;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.LogDebug("Windows detected, using PowerShell as default");
            shell = new PowerShell();
        }
        else
        {
            _logger.LogDebug("Non-Windows OS detected, using sh as default");
            shell = new ShShell();
        }

        return shell;
    }
}