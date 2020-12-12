using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DotRun.Runtime
{
    public class NodeCommand
    {
        public WorkflowContext Context;
        public string FileName;
        public IEnumerable<string> Arguments;
        public IOutput Output;
        public string WorkDirectory;
        public TimeSpan Timeout;
    }

}
