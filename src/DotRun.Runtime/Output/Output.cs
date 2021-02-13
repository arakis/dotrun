// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime
{

    public abstract class Output : IOutput
    {

        public abstract void Log(LogItem itm);

        protected string[] SplitLine(string line)
        {
            line = line.Replace("\r", "");

            if (line.EndsWith("\n"))
                line = line.Substring(0, line.Length - 1);

            return line.Split("\n").Select(s => s.TrimEnd()).ToArray();
        }

    }

}
