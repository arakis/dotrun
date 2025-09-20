using System.Collections.Generic;

namespace DotRun.Runtime.Model.V2;

public class RepositoryPath
{
    /// <summary>
    /// Mount point in the logical namespace.
    /// - Relative mount: "dev" mounts under repository namespace (e.g., repo/dev)
    /// - Absolute mount: "/tools" mounts at namespace root (ignores repository name)
    /// - Empty string: mounts at repository root
    /// </summary>
    public string Mount { get; set; } = "";
    
    /// <summary>
    /// Physical path relative to repository BasePath, or absolute path (starting with / or ~).
    /// Defaults to "." (current directory).
    /// </summary>
    public string Path { get; set; } = ".";
}