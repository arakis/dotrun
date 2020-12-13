// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Threading.Tasks;

namespace DotRun.Runtime
{

    public static class StepExtensions
    {

        public static Task<StepResult> Run(this Step step, StepContext context)
        {
            return StepExecutor.Create(step, context).Run();
        }

        public static void Validate(this Job job)
        {
            if (job.Jobs.Count > 0 && job.Steps.Count > 0)
                throw new Exception("A job with subjobs cannot have steps");
        }

    }

}
