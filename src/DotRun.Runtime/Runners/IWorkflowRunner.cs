using System.Threading.Tasks;

namespace DotRun.Runtime.Runners;

public interface IWorkflowRunner
{
    Task<WorkflowResult> RunAsync(Workflow workflow, RunContext runContext);
}