using System.Collections.Generic;

namespace DotRun.Runtime.Model.V2;

public class RepositoryPath
{
    /// <summary>
    /// Logical name for this path mount. Empty string means mount at repository root.
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// Physical path relative to repository BasePath, or absolute path (starting with / or ~).
    /// Defaults to "." (current directory).
    /// </summary>
    public string Path { get; set; } = ".";
}