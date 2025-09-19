using System.Threading.Tasks;

namespace DotRun.Runtime.Runners;

public interface IStepRunner
{
    Task<StepResult> RunAsync(Step step, StepContext stepContext);
}