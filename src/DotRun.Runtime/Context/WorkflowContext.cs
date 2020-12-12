using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotRun.Runtime
{
    public class WorkflowContext
    {

        public Workflow Workflow { get; internal set; }
        public WorkflowContext(Workflow workflow)
        {
            Workflow = workflow;
        }

        private Dictionary<string, INode> Nodes = new Dictionary<string, INode>();
        public INode GetNode(string name)
        {
            if (name == null)
                name = "local";

            if (Workflow.NodesDict.TryGetValue(name, out var nodeModel))
            {
                lock (Nodes)
                {
                    if (!Nodes.TryGetValue(name, out var node))
                    {
                        node = Node.CreateNode(this, nodeModel).Result;
                        if (!node.Init().Result)
                            throw new Exception($"Unable to init node '{name}'");
                        Nodes.Add(name, node);
                    }

                    return node;
                }
            }
            throw new Exception($"Unable to get node '{name}'");
        }

        public IOutput InternalOutput { get; internal set; } = new ConsoleOutput();

    }

}
