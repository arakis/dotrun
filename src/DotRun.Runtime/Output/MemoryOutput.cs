// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime
{

    public class MemoryOutput : Output
    {

        public List<LogItem> Items { get; } = new List<LogItem>();
        internal IEnumerable<LogItem> InfoItems { get; private set; }
        internal IEnumerable<LogItem> ErrorItems { get; private set; }

        public MemoryOutput()
        {
            InfoItems = Items.Where(x => x.LogLevel == LogLevel.Information);
            ErrorItems = Items.Where(x => x.LogLevel == LogLevel.Error);
        }

        public List<string> InfoLines { get; } = new List<string>();
        public List<string> ErrorLines { get; } = new List<string>();

        public override void Log(LogItem itm)
        {
            Items.Add(itm);
            foreach (var line in SplitLine(itm.Message))
            {
                if (itm.LogLevel == LogLevel.Error)
                    ErrorLines.Add(line);
                else
                    InfoLines.Add(line);
            }
        }
    }

}
