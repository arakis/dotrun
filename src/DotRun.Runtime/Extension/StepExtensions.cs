// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    }

}
