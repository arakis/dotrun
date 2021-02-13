// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime
{
    public class ConsoleOutput : Output
    {

        public override void Log(LogItem itm)
        {
            ConsoleColor? color = null;
            string prefix = "> ";
            if (itm.LogLevel == LogLevel.Error)
            {
                color = ConsoleColor.Red;
                prefix = "! ";
            }

            lock (this)
            {
                UseColor(color, () =>
                {
                    foreach (var line in SplitLine(itm.Message))
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
