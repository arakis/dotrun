// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DotRun.Runtime
{

    public static class YamlHelper
    {
        public static T Deserialize<T>(string yaml)
        {
            if (string.IsNullOrEmpty(yaml))
                return default;
            var obj = new Deserializer().Deserialize<ExpandoObject>(yaml);
            if (obj == null)
                return default;
            var jobj = JObject.FromObject(obj);
            var json = jobj.ToString();

            var seri = new JsonSerializer();
            seri.ContractResolver = new CustomResolver();

            var result = jobj.ToObject<T>(seri);

            return result;
        }
    }

}
