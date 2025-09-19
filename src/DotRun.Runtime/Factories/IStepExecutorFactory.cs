using Microsoft.Extensions.Logging;

namespace DotRun.Runtime.Factories;

public interface IStepExecutorFactory
{
    IStepExecutor CreateStepExecutor(Step step, StepContext context);
    IStepExecutor CreateStepExecutor(Step step, StepContext context, ILogger logger);
}