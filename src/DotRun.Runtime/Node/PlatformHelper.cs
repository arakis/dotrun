using System;

namespace DotRun.Runtime;

public static class PlatformHelper
{
    public static bool IsUnix
        => !IsWindows;

    public static bool IsWindows
        => Environment.OSVersion.Platform.ToString().StartsWith("Win", StringComparison.OrdinalIgnoreCase);
}