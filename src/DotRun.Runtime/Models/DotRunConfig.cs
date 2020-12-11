﻿using System;
using System.Collections.Generic;
using System.IO;

namespace DotRun.Runtime
{

    public class DotRunConfig
    {

        internal static DotRunConfig Load()
        {
            return FromFile(DirectoryHelper.ConfigFile);
        }

        public static DotRunConfig FromFile(string configFilePath)
        {
            var dir = Path.GetDirectoryName(configFilePath);
            return FromFile(configFilePath, dir);
        }

        public static DotRunConfig FromFile(string configFilePath, string configDirectory)
        {
            string content = "";
            if (File.Exists(configFilePath))
                content = File.ReadAllText(configFilePath);

            var cfg = YamlHelper.Deserialize<DotRunConfig>(content);
            if (cfg == null)
                cfg = new DotRunConfig();

            cfg.ConfigDirectory = new DirectoryInfo(configDirectory).FullName;
            cfg.ConfigFile = configFilePath;
            cfg.Content = content;

            return cfg;
        }

        private DotRunConfig() { }

        public string ConfigDirectory { get; internal set; }
        public string ConfigFile { get; internal set; }
        public string Content { get; internal set; }
        public string ProjectsRootDirectory => ConfigDirectory;

        private static DotRunConfig _Current;
        public static DotRunConfig Current
        {
            get
            {
                if (_Current == null)
                    _Current = DotRunConfig.Load();
                return _Current;
            }
        }

        private List<Project> _Projects;
        public List<Project> Projects
        {
            get
            {
                if (_Projects == null)
                    _Projects = LoadProjects(this, ProjectsRootDirectory);
                return _Projects;
            }
        }

        private static List<Project> LoadProjects(DotRunConfig cfg, string projectsDirectory)
        {
            var projects = new List<Project>();
            foreach (var dir in new DirectoryInfo(projectsDirectory).GetDirectories())
                projects.Add(Project.FromFile("", dir.FullName));
            return projects;
        }

    }

}
