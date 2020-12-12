using System;
using System.Collections.Generic;

namespace DotRun.Runtime
{
    public class WorkflowContext
    {

        private Dictionary<string, INode> Nodes = new Dictionary<string, INode>();
        public INode GetNode(string name)
        {
            if (name == null)
                name = "local";

            lock (Nodes)
            {
                if (!Nodes.TryGetValue(name, out var node))
                {
                    node = new DockerNode(this);
                    Nodes.Add(name, node);
                    var result = node.Connect().Result;
                    if (!result)
                        throw new Exception($"Unable to init node '{name}'");
                }

                return node;
            }
        }

        public IOutput InternalOutput { get; internal set; } = new ConsoleOutput();

    }

}
