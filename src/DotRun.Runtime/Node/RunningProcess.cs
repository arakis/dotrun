// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace DotRun.Runtime
{

    public class RunningProcess
    {
        public Task<ProcessResult> CompletedTask { get; set; }
        public Task<bool> StartedTask { get; set; }
        public Task StartedOutput { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
    }

}
