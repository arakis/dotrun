// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime
{

    public abstract class Platform : IPlatform
    {
        public INode Node { get; private set; }
        public abstract PlatformType PlatformType { get; }

        public Platform(INode node)
        {
            Node = node;
        }

        public abstract Task<string> FindExecutablePath(string executable);
        public abstract Task<string> GetHomeDir();
        public abstract Task<string> GetUsername();
        public abstract Task Delete(string path);
    }

}
