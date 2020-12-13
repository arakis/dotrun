// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DotRun.Runtime
{
    public class ConsoleOutput : Output
    {
        public override void Write(string text)
        {
            Console.Write(text);
        }

        public override void WriteLine(string text)
        {
            foreach (var line in SplitLine(text))
            {
                Console.Write("> ");
                Console.WriteLine(line);
            }
        }

        public override void ErrorLine(string text)
        {
            foreach (var line in SplitLine(text))
            {
                Console.Write("! ");
                Console.WriteLine(line);
            }
        }
    }

}
