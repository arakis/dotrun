// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace DotRun.Runtime
{
    public class WorkflowResult
    {
        public Workflow Workflow { get; private set; }
        public IList<JobResult> JobResults { get; set; } = new List<JobResult>();
        public bool Failed => JobResults.Any(x => x.Failed);

        public WorkflowResult(WorkflowContext context)
        {
            Workflow = context.Workflow;
        }
    }

}
