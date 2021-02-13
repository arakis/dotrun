// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace DotRun.Runtime
{
    public static class OutputExtensions
    {
        public static void Info(this IOutput output, string text)
        {
            output.Log(new LogItem(LogLevel.Information, text));
        }

        public static void Trace(this IOutput output, string text)
        {
            output.Log(new LogItem(LogLevel.Trace, text));
        }

        public static void Debug(this IOutput output, string text)
        {
            output.Log(new LogItem(LogLevel.Debug, text));
        }

        public static void Error(this IOutput output, string text)
        {
            output.Log(new LogItem(LogLevel.Error, text));
        }
    }

}
