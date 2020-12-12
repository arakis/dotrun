﻿// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
