// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotRun.Runtime
{

    public class MemoryOutput : Output
    {

        public List<LogEntry<object>> Items { get; } = new List<LogEntry<object>>();
        internal IEnumerable<LogEntry<object>> InfoItems { get; private set; }
        internal IEnumerable<LogEntry<object>> ErrorItems { get; private set; }

        public MemoryOutput()
        {
            InfoItems = Items.Where(x => x.LogLevel == LogLevel.Information);
            ErrorItems = Items.Where(x => x.LogLevel == LogLevel.Error);
        }

        public List<string> InfoLines { get; } = new List<string>();
        public List<string> ErrorLines { get; } = new List<string>();

        public override void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception exception, System.Func<TState, System.Exception, string> formatter)
        {
            Items.Add(new LogEntry<object>(logLevel, null, eventId, state, exception, (st, ex) => formatter((TState)st, ex)));
            foreach (var line in SplitLine(formatter(state, exception)))
            {
                if (logLevel == LogLevel.Error)
                    ErrorLines.Add(line);
                else
                    InfoLines.Add(line);
            }
        }
    }

}
