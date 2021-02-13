// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime
{

    public class ConsoleOutput : Output
    {

        public string Name { get; set; }

        public override void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            ConsoleColor? color = null;

            switch (logLevel)
            {
                case LogLevel.Information:
                    color = ConsoleColor.DarkGreen;
                    break;
                case LogLevel.Warning:
                    color = ConsoleColor.Yellow;
                    break;
                case LogLevel.Error:
                    color = ConsoleColor.Red;
                    break;
                case LogLevel.Trace:
                    color = ConsoleColor.DarkYellow;
                    break;
                case LogLevel.Debug:
                    color = ConsoleColor.DarkGray;
                    break;
            }

            lock (this)
            {
                UseColor(color, () =>
                {
                    if (!string.IsNullOrEmpty(Name))
                        Console.Write("[" + Name + "] ");

                    Console.WriteLine(formatter(state, exception));
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
