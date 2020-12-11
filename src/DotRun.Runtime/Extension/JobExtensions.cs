using System.Threading.Tasks;

namespace DotRun.Runtime
{

    public static class JobExtensions
    {

        public static async Task<JobResult> Run(this Job job, JobContext context)
        {
            foreach (var step in job.Steps)
            {
                await step.Run(new StepContext(step, context));
            }

            return new JobResult();
        }

    }

}
