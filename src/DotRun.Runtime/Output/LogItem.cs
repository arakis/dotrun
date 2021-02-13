// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace DotRun.Runtime
{
    public readonly struct LogItem
    {
        public readonly LogLevel LogLevel;
        public readonly string Message;
        public readonly object[] Values;

        public LogItem(LogLevel logLevel, string message, object[] values = null)
        {
            LogLevel = logLevel;
            Message = message;
            Values = values;
        }
    }

}
