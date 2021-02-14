// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime
{
    public static class WorkflowExtensions
    {

        public static Task<WorkflowResult> Run(this Workflow workflow)
        {
            return Run(workflow, new WorkflowContext(workflow));
        }

        public static async Task<WorkflowResult> Run(this Workflow workflow, WorkflowContext context)
        {
            context.InternalOutput.LogInformation($"*** Worflow started: {workflow.Name} ***");

            var workflowResult = new WorkflowResult(context);
            foreach (var job in workflow.Jobs)
            {
                var jobResult = await job.Run(new JobContext(job, context));
                workflowResult.JobResults.Add(jobResult);
            }

            context.InternalOutput.LogInformation($"*** Worflow finished: {workflow.Name} ***");

            return workflowResult;
        }

    }

}
