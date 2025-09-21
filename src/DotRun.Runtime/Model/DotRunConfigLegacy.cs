// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DotRun.Runtime;

public class DotRunConfigLegacy
{
    //internal static DotRunConfig Load()
    //{

    //    return FromFile(DirectoryHelper.ConfigFile);
    //}

    public static DotRunConfigLegacy FromFile(string configFilePath)
    {
        var dir = Path.GetDirectoryName(configFilePath);
        return FromFile(configFilePath, dir);
    }

    public static DotRunConfigLegacy FromFile(string configFilePath, string configDirectory)
    {
        if (!string.IsNullOrEmpty(configFilePath))
            configFilePath = DirectoryHelper.GetAbsoluteLocalPath(configFilePath);

        if (!string.IsNullOrEmpty(configDirectory))
            configDirectory = DirectoryHelper.GetAbsoluteLocalPath(configDirectory);

        string content = "";
        if (File.Exists(configFilePath))
            content = File.ReadAllText(configFilePath);

        var cfg = YamlHelper.Deserialize<DotRunConfigLegacy>(content);
        if (cfg == null)
            cfg = new DotRunConfigLegacy();

        cfg.ConfigDirectory = new DirectoryInfo(configDirectory).FullName;
        cfg.ConfigFile = configFilePath;
        cfg.Content = content;
        if (string.IsNullOrEmpty(cfg.ProjectsRootDirectory))
            cfg.ProjectsRootDirectory = cfg.ConfigDirectory;

        cfg.ProjectsRootDirectory = DirectoryHelper.GetAbsoluteLocalPath(cfg.ProjectsRootDirectory, cfg.ConfigDirectory);

        return cfg;
    }

    private DotRunConfigLegacy()
    {
    }

    public string ConfigDirectory { get; internal set; }
    public string ConfigFile { get; internal set; }
    public string Content { get; internal set; }
    public string ProjectsRootDirectory { get; set; }

    private ProjectsCollection _Projects;

    public ProjectsCollection Projects
    {
        get
        {
            if (_Projects == null)
                _Projects = LoadProjects(this, ProjectsRootDirectory);
            return _Projects;
        }
    }

    private static ProjectsCollection LoadProjects(DotRunConfigLegacy cfg, string projectsDirectory)
    {
        projectsDirectory = DirectoryHelper.GetAbsoluteLocalPath(projectsDirectory);
        var projects = new ProjectsCollection();
        foreach (var dir in new DirectoryInfo(projectsDirectory).GetDirectories())
            projects.Add(Project.FromFile("", dir.FullName));
        return projects;
    }

    public string SecretsFile { get; set; } = "~/dotrun.secrets.yaml";

    private static List<Secret> LoadSecrets(DotRunConfigLegacy cfg)
    {
        if (!File.Exists(cfg.SecretsFile))
            return new List<Secret>();

        var secrets = YamlHelper.Deserialize<SecretsInfo>(File.ReadAllText(cfg.SecretsFile));
        return secrets.Secrets;
    }

    private List<Secret> _Secrets;

    public List<Secret> Secrets
    {
        get
        {
            if (_Secrets == null)
                _Secrets = LoadSecrets(this);
            return _Secrets;
        }
    }

    public Secret GetSecret(string name)
    {
        return Secrets.FirstOrDefault(s => s.Name?.ToLower() == name);
    }
}