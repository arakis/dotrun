// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime
{
    public class ConsoleOutput : Output
    {

        public override void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            ConsoleColor? color = null;
            string prefix = null;

            switch (logLevel)
            {
                case LogLevel.Information:
                    color = ConsoleColor.DarkGreen;
                    prefix = "> ";
                    break;
                case LogLevel.Warning:
                    color = ConsoleColor.Yellow;
                    prefix = "? ";
                    break;
                case LogLevel.Error:
                    color = ConsoleColor.Red;
                    prefix = "! ";
                    break;
                case LogLevel.Trace:
                    color = ConsoleColor.DarkYellow;
                    prefix = ": ";
                    break;
                case LogLevel.Debug:
                    color = ConsoleColor.DarkGray;
                    prefix = "% ";
                    break;
            }

            lock (this)
            {
                UseColor(color, () =>
                {
                    foreach (var line in SplitLine(formatter(state, exception)))
                    {
                        Console.Write(prefix);
                        Console.WriteLine(line);
                    }
                });
            }
        }

        private void UseColor(ConsoleColor? color, Action action)
        {
            if (color == null)
            {
                action();
                return;
            }

            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = (ConsoleColor)color;
            try
            {
                action();
            }
            finally
            {
                Console.ForegroundColor = oldColor;
            }
        }

    }

}
