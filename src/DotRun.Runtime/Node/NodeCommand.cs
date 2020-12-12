// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public Dictionary<string, string> Env = new Dictionary<string, string>();
        public IOutput Output;
        public string WorkDirectory;
        public TimeSpan Timeout;
    }

}
