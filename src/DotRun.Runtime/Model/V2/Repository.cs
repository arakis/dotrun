using System.Collections.Generic;

namespace DotRun.Runtime.Model.V2;

public class Repository
{
    /// <summary>
    /// Logical name for this repository. Empty string means global root namespace.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Type of repository (FileSystem, Git, etc.)
    /// </summary>
    public RepositoryType Type { get; set; } = RepositoryType.FileSystem;

    /// <summary>
    /// Base path for relative path resolution. Can be relative or absolute.
    /// </summary>
    public string BasePath { get; set; } = ".";

    /// <summary>
    /// List of path mounts in this repository.
    /// If null or empty, the entire BasePath is mounted at repository root.
    /// </summary>
    public List<RepositoryPath> Paths { get; set; } = new();
}