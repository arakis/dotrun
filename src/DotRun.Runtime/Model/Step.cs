// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace DotRun.Runtime
{
    public class Step
    {
        public Job Job { get; set; }
        public string Name { get; set; }
        public string Run { get; set; }
        public string Uses { get; set; }
        public Dictionary<string, object> With { get; set; } = new Dictionary<string, object>();
        public string WorkDirectory { get; set; }
        public string Node { get; set; }
        public string Shell { get; set; }
    }

}
