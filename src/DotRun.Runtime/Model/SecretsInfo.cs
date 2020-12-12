// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotRun.Runtime
{

    public class SecretsInfo
    {
        public string Kind { get; set; }
        public List<Secret> Secrets { get; set; }
    }

}
