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
