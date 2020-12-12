// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DotRun.Runtime
{
    internal class CustomResolver : CamelCasePropertyNamesContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty prop = base.CreateProperty(member, memberSerialization);

            //prop.Writable = true;

            // TODO?

            return prop;
        }
    }

}
