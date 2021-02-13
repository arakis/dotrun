// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotRun.Runtime
{

    public static class JobExtensions
    {

        public static async Task<JobResult> Run(this Job job, JobContext context)
        {
            if (job.Jobs.Count > 0)
            {
                var tasks = new List<Task<JobResult>>();
                foreach (var child in job.Jobs)
                    tasks.Add(RunInternal(child, new JobContext(child, context.WorkflowContext)));
                Task.WaitAll(tasks.ToArray());
            }
            else
            {
                return await RunInternal(job, context);
            }

            return new JobResult(context);
        }

        private static async Task<JobResult> RunInternal(this Job job, JobContext context)
        {
            var jobResult = new JobResult(context);
            foreach (var step in job.Steps)
            {
                var stepResult = await step.Run(new StepContext(step, context));
                jobResult.StepResults.Add(stepResult);
            }

            return jobResult;
        }

    }

}
