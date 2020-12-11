namespace DotRun.Runtime
{

    public class JobContext
    {

        public WorkflowContext WorkflowContext { get; private set; }
        public Job Job { get; private set; }

        public JobContext(Job job, WorkflowContext context)
        {
            Job = job;
            WorkflowContext = context;
        }

    }

}
