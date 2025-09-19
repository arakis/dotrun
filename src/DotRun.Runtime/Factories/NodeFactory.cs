using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime.Factories;

public class NodeFactory : INodeFactory
{
    private readonly ILogger<NodeFactory> _logger;

    public NodeFactory(ILogger<NodeFactory> logger)
    {
        _logger = logger;
    }

    public async Task<INode> CreateNodeAsync(WorkflowContext context, NodeModel model)
    {
        if (model == null)
        {
            _logger.LogError("NodeModel is null");
            throw new ArgumentNullException(nameof(model));
        }

        _logger.LogDebug("Creating node of type: {NodeType}", model.Type);

        INode node = model.Type switch
        {
            NodeType.Local => new LocalNode(context, model),
            NodeType.Docker => new DockerNode(context, model),
            NodeType.Ssh => new SshNode(context, model),
            _ => throw new NotSupportedException($"Node type '{model.Type}' is not supported")
        };

        if (!await node.Init())
        {
            throw new InvalidOperationException($"Failed to initialize {model.Type} node");
        }

        _logger.LogInformation("Created and initialized {NodeType} node", model.Type);
        return node;
    }
}