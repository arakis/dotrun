using System;

namespace DotRun.Runtime
{

    public class ExecutionContext
    {
        public IStepEnvironment GetEnvironment(string name)
        {
            throw new NotImplementedException();
        }
    }

    public class StepExecutionContext
    {
        public ExecutionContext ExecutionContext { get; set; }
        public IStepEnvironment Environment { get; set; }
        public Step Step { get; set; }
    }

}
