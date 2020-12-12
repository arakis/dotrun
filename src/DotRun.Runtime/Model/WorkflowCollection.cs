using System.Collections.Generic;
using System.Linq;

namespace DotRun.Runtime
{
    public class WorkflowCollection : List<Workflow>
    {

        public Workflow this[string name]
            => this.FirstOrDefault(w => w.Name?.ToLower() == name.ToLower());

    }

}
