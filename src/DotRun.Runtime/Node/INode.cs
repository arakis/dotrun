// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DotRun.Runtime
{

    public interface INode : IDisposable, IAsyncDisposable
    {
        IPlatform Platform { get; }
        Task<bool> Init();

        Task WriteFile(StepContext context, string path, Stream source);

        RunningProcess ExecuteCommand(NodeCommand cmd);
        Task<string> FindExecutablePath(string executable);
        Task<string> GetHomeDir();
        Task<string> GetUserName();

        IShell CreateShell(string name);
    }

}
