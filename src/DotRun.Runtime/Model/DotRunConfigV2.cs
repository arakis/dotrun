using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DotRun.Runtime.Model;

public class DotRunConfigV2
{
    /// <summary>
    /// Path to the secrets file
    /// </summary>
    public string SecretsFile { get; set; } = "~/dotrun.secrets.yaml";

    /// <summary>
    /// List of repositories containing workflows
    /// </summary>
    public List<Repository> Repositories { get; set; } = new();

    // Internal properties for config management
    internal string ConfigDirectory { get; set; }
    internal string ConfigFile { get; set; }
    internal string Content { get; set; }

    public static DotRunConfigV2 FromFile(string configFilePath)
    {
        var dir = Path.GetDirectoryName(configFilePath);
        return FromFile(configFilePath, dir);
    }

    public static DotRunConfigV2 FromFile(string configFilePath, string configDirectory)
    {
        if (!string.IsNullOrEmpty(configFilePath))
            configFilePath = DirectoryHelper.GetAbsoluteLocalPath(configFilePath);

        if (!string.IsNullOrEmpty(configDirectory))
            configDirectory = DirectoryHelper.GetAbsoluteLocalPath(configDirectory);

        string content = "";
        if (File.Exists(configFilePath))
            content = File.ReadAllText(configFilePath);

        var cfg = YamlHelper.Deserialize<DotRunConfigV2>(content);
        if (cfg == null)
            cfg = new DotRunConfigV2();

        cfg.ConfigDirectory = new DirectoryInfo(configDirectory).FullName;
        cfg.ConfigFile = configFilePath;
        cfg.Content = content;

        // Expand paths in repositories
        cfg.NormalizePaths();

        return cfg;
    }

    private void NormalizePaths()
    {
        foreach (var repo in Repositories)
        {
            // Expand BasePath if needed
            if (!string.IsNullOrEmpty(repo.BasePath))
            {
                repo.BasePath = DirectoryHelper.GetAbsoluteLocalPath(repo.BasePath, ConfigDirectory);
            }

            // If no paths specified, create a default root mount
            if (repo.Paths == null || repo.Paths.Count == 0)
            {
                repo.Paths = new List<RepositoryPath>
                {
                    new RepositoryPath { Mount = "", Path = "." }
                };
            }
        }
    }

    /// <summary>
    /// Get all workflow mount points with their resolved physical paths
    /// </summary>
    public IEnumerable<(string LogicalPath, string PhysicalPath)> GetAllMountPoints()
    {
        foreach (var repo in Repositories)
        {
            foreach (var path in repo.Paths)
            {
                var logicalPath = BuildLogicalPath(repo.Name, path.Mount);
                var physicalPath = ResolvePath(repo, path);
                yield return (logicalPath, physicalPath);
            }
        }
    }

    private string BuildLogicalPath(string repoName, string mount)
    {
        // Absolute mount - starts with /
        if (mount?.StartsWith("/") == true)
        {
            // Return without leading slash for consistency
            return mount.TrimStart('/');
        }

        // Relative mount under repository
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(repoName))
            parts.Add(repoName);
        if (!string.IsNullOrEmpty(mount))
            parts.Add(mount);

        return parts.Count > 0 ? string.Join("/", parts) : "";
    }

    private string ResolvePath(Repository repo, RepositoryPath path)
    {
        if (IsAbsolutePath(path.Path))
        {
            // Absolute path - expand ~ and use as-is
            return DirectoryHelper.GetAbsoluteLocalPath(path.Path);
        }
        else
        {
            // Relative path - relative to repository BasePath
            return Path.Combine(repo.BasePath, path.Path);
        }
    }

    private bool IsAbsolutePath(string path)
    {
        return path.StartsWith("/") || path.StartsWith("~");
    }
}