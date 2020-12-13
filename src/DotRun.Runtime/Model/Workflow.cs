// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace DotRun.Runtime
{

    public class Workflow
    {
        public Project Project { get; internal set; }

        private IList<NodeModel> _Nodes;

        [JsonIgnore]
        public IList<NodeModel> Nodes
        {
            get
            {
                if (_Nodes == null)
                    _Nodes = NodesDict.Values.ToList();
                return _Nodes;
            }
        }

        [JsonProperty("nodes")]
        internal Dictionary<string, NodeModel> NodesDict { get; set; } = new Dictionary<string, NodeModel>();

        private IList<Job> _Jobs;

        [JsonIgnore]
        public IList<Job> Jobs
        {
            get
            {
                if (_Jobs == null)
                    _Jobs = JobsDict.Values.ToList();
                return _Jobs;
            }
        }

        [JsonProperty("jobs")]
        internal Dictionary<string, Job> JobsDict { get; set; }

        [JsonProperty]
        internal string Kind { get; set; }
        internal string WorkflowFilePath { get; set; }
        public string Name { get; set; }

        public static Workflow FromFile(Project project, string workflowFilePath)
        {
            var dir = Path.GetDirectoryName(workflowFilePath);
            var content = File.ReadAllText(workflowFilePath);
            return FromString(project, content, workflowFilePath);
        }

        public static Workflow FromString(Project project, string content, string workflowFilePath)
        {
            var wf = YamlHelper.Deserialize<Workflow>(content);
            if (wf.Kind?.ToLower() != "workflow")
                return null;

            wf.Project = project;
            wf.WorkflowFilePath = workflowFilePath;
            if (string.IsNullOrEmpty(wf.Name))
                wf.Name = Path.GetFileNameWithoutExtension(workflowFilePath);

            foreach (var entry in wf.JobsDict)
            {
                entry.Value.Name = entry.Key;
                entry.Value.Init();
            }

            if (!wf.NodesDict.ContainsKey("local"))
                wf.NodesDict.Add("local", new NodeModel { Type = NodeType.Local });

            foreach (var entry in wf.NodesDict)
                entry.Value.Name = entry.Key;

            return wf;
        }

    }

}
