using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DotRun.Runtime
{

    public interface INode : IDisposable, IAsyncDisposable
    {
        Task<bool> Connect();

        Task WriteFile(StepContext context, string path, Stream source);

        RunningProcess ExecuteCommand(NodeCommand cmd);

        IShell CreateShell(string name);
    }

}
