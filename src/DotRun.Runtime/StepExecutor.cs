using System;
using System.Threading.Tasks;

namespace DotRun.Runtime
{
    public class StepExecutor : IStepExecutor
    {

        public Step Step { get; private set; }
        public StepContext Context { get; private set; }

        public INode Node { get; private set; }

        public IShell Shell { get; private set; }
        public IOutput Output { get; private set; }

        public static IStepExecutor Create(Step step, StepContext context)
        {
            var executor = new StepExecutor(step, context);
            executor.Node = context.WorkflowContext.GetNode(step.Node);
            executor.Shell = context.Node.CreateShell(step.Shell);
            executor.Output = new ConsoleOutput();
            return executor;
        }

        private StepExecutor(Step step, StepContext context)
        {
            Step = step;
            Context = context;
        }

        public Task<StepResult> Run()
        {
            return RunInternal();
        }

        internal Task<StepResult> RunInternal()
        {
            return Shell.Execute(Context, Output);
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }
    }

}
