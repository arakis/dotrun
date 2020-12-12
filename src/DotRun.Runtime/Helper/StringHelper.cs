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
    public static class StringHelper
    {
        private static Random Random = new Random();
        public static string RandomString()
        {
            return Random.Next(int.MaxValue).ToString("X8");
        }
    }

}
