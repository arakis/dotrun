using System.Threading.Tasks;

namespace DotRun.Runtime
{

    public static class JobExtensions
    {

        public static async Task<JobExecutionResult> Run(this Job job, JobExecutionContext context)
        {
            foreach (var step in job.Steps)
            {
                await step.Run(new StepExecutionContext());
            }

            return new JobExecutionResult();
        }

    }

}
