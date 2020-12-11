using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DotRun.Runtime
{

    public interface INode
    {
        Task WriteFile(StepContext context, string path, Stream source);

        Task<ProcessResult> ExecuteCommand(NodeCommand cmd);

        IShell CreateShell(string name);
    }

}
