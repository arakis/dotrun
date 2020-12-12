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
