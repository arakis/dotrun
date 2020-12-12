// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace DotRun.Runtime
{

    public class MemoryOutput : IOutput
    {
        public void Write(string text)
        {
        }

        public List<string> Lines { get; } = new List<string>();

        public void WriteLine(string text)
        {
            Lines.Add(text);
        }

        public List<string> Errors { get; } = new List<string>();
        public void ErrorLine(string text)
        {
            Errors.Add(text);
        }
    }

}
