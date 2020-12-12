// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace DotRun.Runtime
{
    public class ProjectsCollection : List<Project>
    {

        public Project this[string name]
            => this.FirstOrDefault(p => p.Name?.ToLower() == name.ToLower());

    }

}
