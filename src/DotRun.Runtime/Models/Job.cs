using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DotRun.Runtime
{

    public class Job
    {
        public Workflow Workflow { get; init; }

        [JsonProperty]
        public List<Step> Steps { get; internal set; }

        public string Name { get; set; }
    }

}
