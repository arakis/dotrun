﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace DotRun.Runtime
{

    public enum NodeType
    {
        Local,
        Docker
    }

    public class NodeModel
    {
        public NodeType Type { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }

        [JsonProperty("image-pull-auth")]
        public string ImagePullAuth { get; set; }
    }

    public class Workflow
    {
        public Project Project { get; internal set; }

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
        internal Dictionary<string, NodeModel> NodesDict { get; set; }

        [JsonProperty("jobs")]
        internal Dictionary<string, Job> JobsDict { get; set; }

        [JsonProperty]
        internal string Kind { get; set; }
        internal string WorkflowFilePath { get; set; }

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

            foreach (var entry in wf.JobsDict)
                entry.Value.Name = entry.Key;

            foreach (var entry in wf.NodesDict)
                entry.Value.Name = entry.Key;

            return wf;
        }

    }

}
