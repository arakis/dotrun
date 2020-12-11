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
                    node = new LocalNode();
                    Nodes.Add(name, node);
                }
                return node;
            }
        }

    }

}
