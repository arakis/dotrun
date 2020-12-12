// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
