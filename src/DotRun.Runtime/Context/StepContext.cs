using System;

namespace DotRun.Runtime
{

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

        public INode Node
            => WorkflowContext.GetNode(Step.Node);
    }

}
