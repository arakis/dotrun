using System.Threading.Tasks;
using System.Threading;

namespace DotRun.Runtime
{
    public class ProcessResult
    {
        public bool Completed;
        public int? ExitCode;
    }

    public class RunningProcess
    {
        public Task<ProcessResult> CompletedTask { get; set; }
        public Task<bool> StartedTask { get; set; }
        public Task StartedOutput { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
    }

}
