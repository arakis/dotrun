// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace DotRun.Runtime
{
    public static class OutputExtensions
    {
        public static void WriteLine(this IOutput output, string text)
        {
            output.Log(new LogItem(LogLevel.Information, text));
        }

        public static void ErrorLine(this IOutput output, string text)
        {
            output.Log(new LogItem(LogLevel.Error, text));
        }
    }

}
