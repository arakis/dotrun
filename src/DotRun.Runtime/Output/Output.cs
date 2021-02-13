// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime
{

    public abstract class Output : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }

        protected string[] SplitLine(string line)
        {
            line = line.Replace("\r", "");

            if (line.EndsWith("\n"))
                line = line.Substring(0, line.Length - 1);

            return line.Split("\n").Select(s => s.TrimEnd()).ToArray();
        }

    }

}
