using System;
using System.Collections.Generic;

namespace DotRun.Runtime
{

    public class WorkflowContext
    {
        private Dictionary<string, IStepEnvironment> Environments = new Dictionary<string, IStepEnvironment>();
        public IStepEnvironment GetEnvironment(string name)
        {
            if (name == null)
                name = "local";

            lock (Environments)
            {
                if (!Environments.TryGetValue(name, out var env))
                {
                    env = new LocalEnvironment();
                    Environments.Add(name, env);
                }
                return env;
            }
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
