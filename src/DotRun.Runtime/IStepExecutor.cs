using System.Threading.Tasks;

namespace DotRun.Runtime
{
    public interface IStepExecutor
    {
        public Step Step { get; }
        public StepContext Context { get; }
        public INode Node { get; }
        public IShell Shell { get; }
        public IOutput Output { get; }
        public void Abort();
        public Task<StepResult> Run();
    }

}
