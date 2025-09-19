using System.Threading.Tasks;

namespace DotRun.Runtime.Factories;

public interface INodeFactory
{
    Task<INode> CreateNodeAsync(WorkflowContext context, NodeModel nodeModel);
}