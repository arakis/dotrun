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
