// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotRun.Runtime.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime;

public class WorkflowContext
{
    public Workflow Workflow { get; }
    public RunContext RunContext { get; }

    public WorkflowContext(Workflow workflow, RunContext runContext)
    {
        Workflow = workflow;
        RunContext = runContext;
    }

    private Dictionary<string, INode> Nodes = new Dictionary<string, INode>();

    public INode GetNode(string name)
    {
        name = name.ReplaceVariables(this);

        if (name == null)
            name = "local";

        if (Workflow.NodesDict.TryGetValue(name, out var nodeModel))
        {
            lock (Nodes)
            {
                if (!Nodes.TryGetValue(name, out var node))
                {
                    // Use NodeFactory from DI
                    var nodeFactory = RunContext.ServiceProvider.GetRequiredService<INodeFactory>();
                    node = nodeFactory.CreateNodeAsync(this, nodeModel).Result;
                    Nodes.Add(name, node);
                }

                return node;
            }
        }

        throw new Exception($"Unable to get node '{name}'");
    }

    public ILogger InternalOutput { get; internal set; } = new ConsoleOutput();
}