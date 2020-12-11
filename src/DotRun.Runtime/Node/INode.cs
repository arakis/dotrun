using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DotRun.Runtime
{
    public interface INode
    {
        Task WriteFile(StepContext context, string path, Stream source);

        Task ExecuteCommand(StepContext context, string proc, IEnumerable<string> args, IOutput output);

        public IShell CreateShell(string name);
    }

}
