// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace DotRun.Runtime
{

    public class Job
    {
        public Workflow Workflow { get; init; }

        [JsonProperty]
        public List<Step> Steps { get; internal set; } = new List<Step>();

        public string Name { get; set; }

        private IList<Job> _Jobs;

        [JsonIgnore]
        public IList<Job> Jobs
        {
            get
            {
                if (_Jobs == null)
                    _Jobs = JobsDict.Values.ToList();
                return _Jobs;
            }
        }

        [JsonProperty("jobs")]
        internal Dictionary<string, Job> JobsDict { get; set; } = new Dictionary<string, Job>();

        [JsonProperty("max-parallel")]
        public int MaxParallel { get; set; } = 1;

        internal void Init()
        {
            foreach (var entry in JobsDict)
                entry.Value.Name = entry.Key;

            this.Validate();
        }

    }

}
