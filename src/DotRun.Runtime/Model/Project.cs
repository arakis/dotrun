using System.Collections.Generic;
using System.IO;

namespace DotRun.Runtime
{

    public class Project
    {

        public static Project FromFile(string projectConfigFilePath)
        {
            var dir = Path.GetDirectoryName(projectConfigFilePath);
            return FromFile(projectConfigFilePath, dir);
        }

        public static Project FromFile(string projectConfigFilePath, string projectConfigDirectory)
        {
            string content;
            if (string.IsNullOrEmpty(projectConfigFilePath))
                content = "";
            else
                content = File.ReadAllText(projectConfigFilePath);

            var cfg = new Project
            {
                ProjectDirectory = new DirectoryInfo(projectConfigDirectory).FullName,
                ProjectFile = projectConfigFilePath,
                Content = content,
                Name = Path.GetFileName(projectConfigDirectory),
            };
            return cfg;
        }

        private Project() { }

        public string ProjectDirectory { get; init; }
        public string ProjectFile { get; init; }
        public string Content { get; init; }
        public string Name { get; init; }

        private WorkflowCollection _Workflows;
        public WorkflowCollection Workflows
        {
            get
            {
                if (_Workflows == null)
                    _Workflows = LoadWorkflows(this);
                return _Workflows;
            }
        }

        private static WorkflowCollection LoadWorkflows(Project project)
        {
            var workflows = new WorkflowCollection();

            foreach (var file in new DirectoryInfo(project.ProjectDirectory).GetFiles())
            {
                var wf = Workflow.FromFile(project, file.FullName);
                if (wf != null)
                    workflows.Add(wf);
            }

            return workflows;
        }

    }

}
