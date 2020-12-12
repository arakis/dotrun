// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotRun.Runtime
{

    public class Secret
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public string Username { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }

        public string EnvVarName { get; set; }
        public string EnvVarValue { get; set; }
    }

}
