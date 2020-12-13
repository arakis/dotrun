// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace DotRun.Runtime
{

    public class MemoryOutput : Output
    {
        public override void Write(string text)
        {
        }

        public List<string> Lines { get; } = new List<string>();

        public override void WriteLine(string text)
        {
            foreach (var line in SplitLine(text))
                Lines.Add(line);
        }

        public List<string> Errors { get; } = new List<string>();
        public override void ErrorLine(string text)
        {
            foreach (var line in SplitLine(text))
                Errors.Add(line);
        }
    }

}
