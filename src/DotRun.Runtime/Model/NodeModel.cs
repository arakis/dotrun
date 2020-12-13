// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace DotRun.Runtime
{
    public class NodeModel
    {
        public NodeType Type { get; set; }
        public string Name { get; set; }
        public string Host { get; set; }
        public string Username { get; set; }
        public string KeyFile { get; set; }
        public string Image { get; set; }

        [JsonProperty("image-pull-auth")]
        public string ImagePullAuth { get; set; }
    }

}
