// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DotRun.Runtime
{

    public class StepContext
    {

        public StepContext(Step step, JobContext jobContext)
        {
            Step = step;
            JobContext = jobContext;
        }

        public JobContext JobContext { get; set; }

        public WorkflowContext WorkflowContext => JobContext.WorkflowContext;

        public Step Step { get; set; }

        public INode Node
            => WorkflowContext.GetNode(Step.Node);
    }

}
