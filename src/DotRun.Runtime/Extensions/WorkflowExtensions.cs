using System.Threading.Tasks;

namespace DotRun.Runtime
{
    public static class WorkflowExtensions
    {

        public static Task<WorkflowResult> Run(this Workflow workflow)
        {
            return Run(workflow, new WorkflowContext());
        }

        public static async Task<WorkflowResult> Run(this Workflow workflow, WorkflowContext context)
        {
            foreach (var job in workflow.Jobs)
            {
                var status = await job.Run(new JobContext(job, context));
            }

            return new WorkflowResult();
        }

    }

}
