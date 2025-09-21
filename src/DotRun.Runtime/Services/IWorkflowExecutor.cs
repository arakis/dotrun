using System.Threading;
using System.Threading.Tasks;

namespace DotRun.Runtime.Services;

public interface IWorkflowExecutor
{
    Task<WorkflowResult> ExecuteAsync(
        DotRunConfigLegacy configLegacy,
        string projectName,
        string workflowName,
        CancellationToken cancellationToken = default);
}