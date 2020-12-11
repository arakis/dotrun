using System;
using System.Collections.Generic;

namespace DotRun.Runtime
{
    public class ShellCommand
    {
        public StepContext context;
        public string proc;
        public IEnumerable<string> args;
        public IOutput output;
        public string workdirectory;
        public TimeSpan timeout;
    }

}
