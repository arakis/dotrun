// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace DotRun.Runtime
{

    public class JobResult
    {
        public Job Job { get; private set; }
        public IList<StepResult> StepResults { get; set; } = new List<StepResult>();

        public bool Failed => StepResults.Any(x => x.Failed);

        public JobResult(JobContext context)
        {
            Job = context.Job;
        }
    }

}
