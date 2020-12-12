// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotRun.Runtime
{

    public class JobContext
    {

        public WorkflowContext WorkflowContext { get; private set; }
        public Job Job { get; private set; }

        public JobContext(Job job, WorkflowContext context)
        {
            Job = job;
            WorkflowContext = context;
        }

    }

}
