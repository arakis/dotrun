// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DotRun.Runtime
{
    public class ConsoleOutput : IOutput
    {
        public void Write(string text)
        {
            Console.Write(text);
        }

        public void WriteLine(string text)
        {
            Console.Write("> ");
            Console.WriteLine(text);
        }

        public void ErrorLine(string text)
        {
            Console.Write("! ");
            Console.WriteLine(text);
        }
    }

}
