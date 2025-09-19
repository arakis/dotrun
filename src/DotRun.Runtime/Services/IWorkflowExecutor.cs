using System.Threading;
using System.Threading.Tasks;

namespace DotRun.Runtime.Services;

public interface IWorkflowExecutor
{
    Task<WorkflowResult> ExecuteAsync(
        DotRunConfig config,
        string projectName,
        string workflowName,
        CancellationToken cancellationToken = default);
}