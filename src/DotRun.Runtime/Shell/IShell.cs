// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime
{
    public interface IShell
    {

        Task<StepResult> Execute(StepContext context, ILogger output);

    }

}
