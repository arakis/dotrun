﻿// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace DotRun.Runtime
{
    public class NullOutput : IOutput
    {
        public void Write(string text)
        {
        }

        public void WriteLine(string text)
        {
        }

        public void ErrorLine(string text)
        {
        }
    }

}
