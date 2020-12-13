// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DotRun.Runtime
{

    public abstract class UseAction : IUseAction
    {

        public StepContext Context { get; private set; }

        public UseAction(StepContext context)
        {
            Context = context;
        }

        public INode Node => Context.Node;
        public Step Step => Context.Step;

        public abstract Task Run();

        public static IUseAction Create(StepContext context)
        {
            return context.Step.Uses?.ToLower() switch
            {
                "dotrun/checkout" => new GitCloneUseAction(context),
                "dotrun/store-artifacts" => new StoreArtifactUseAction(context),
                _ => null,
            };
        }
    }

}
