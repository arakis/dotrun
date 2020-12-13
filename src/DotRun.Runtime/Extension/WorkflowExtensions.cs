﻿// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            foreach (var job in workflow.Jobs)
            {
                var status = await job.Run(new JobContext(job, context));
            }

            return new WorkflowResult();
        }

    }

}
