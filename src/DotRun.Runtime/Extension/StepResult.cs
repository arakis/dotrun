// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotRun.Runtime
{
    public class StepResult
    {
        public Step Step { get; private set; }
        public bool Failed { get; internal set; }
        public int? ExitCode { get; internal set; }

        public StepResult(StepContext context)
        {
            Step = context.Step;
        }
    }

}
