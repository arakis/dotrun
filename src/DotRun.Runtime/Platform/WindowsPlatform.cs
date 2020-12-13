﻿// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
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

    public class WindowsPlatform : Platform
    {
        public WindowsPlatform(INode node) : base(node)
        {
        }

        public override async Task<string> FindExecutablePath(string executable)
        {
            var output = new MemoryOutput();
            await Node.ExecuteCommand(new NodeCommand
            {
                FileName = "cmd.exe",
                Arguments = new string[] { "/c", "where " + executable },
                Output = output,
            }).CompletedTask;

            return output.Lines.FirstOrDefault();
        }

        public override Task<string> GetHomeDir()
        {
            throw new NotImplementedException();
        }

        public override Task<string> GetUsername()
        {
            throw new NotImplementedException();
        }
    }

}
