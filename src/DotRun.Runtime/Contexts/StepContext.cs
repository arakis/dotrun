using System;

namespace DotRun.Runtime
{

    public class WorkflowContext
    {
        public IStepEnvironment GetEnvironment(string name)
        {
            throw new NotImplementedException();
        }
    }

    public class StepContext
    {

        public StepContext(Step step, JobContext jobContext)
        {
            Step = step;
            JobContext = jobContext;
        }

        public JobContext JobContext { get; set; }

        public WorkflowContext WorkflowContext => JobContext.WorkflowContext;

        public Step Step { get; set; }

        public IStepEnvironment Environment
            => WorkflowContext.GetEnvironment(Step.Environment);
    }

}
