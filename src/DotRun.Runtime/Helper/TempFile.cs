// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace DotRun.Runtime
{

    internal class TempFile : IDisposable
    {

        public string FilePath { get; init; }

        public void Dispose()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }

    }
}
