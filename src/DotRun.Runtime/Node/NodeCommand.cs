using System;
using System.Collections.Generic;

namespace DotRun.Runtime
{
    public class NodeCommand
    {
        public StepContext Context;
        public string FileName;
        public IEnumerable<string> Arguments;
        public IOutput Output;
        public string WorkDirectory;
        public TimeSpan Timeout;
    }

}
