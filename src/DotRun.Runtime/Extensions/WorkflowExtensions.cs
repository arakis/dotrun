using System.Threading.Tasks;

namespace DotRun.Runtime
{
    public static class WorkflowExtensions
    {

        public static Task<WorkflowExecutionResult> Run(this Workflow workflow)
        {
            return Run(workflow, new WorkflowExecutionContext());
        }

        public static async Task<WorkflowExecutionResult> Run(this Workflow workflow, WorkflowExecutionContext context)
        {
            foreach (var job in workflow.Jobs)
            {
                var status = await job.Run(new JobExecutionContext());
            }

            return new WorkflowExecutionResult();
        }

    }

}
