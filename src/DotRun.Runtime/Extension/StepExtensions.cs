// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DotRun.Runtime;

public static class StepExtensions
{
    public static void Validate(this Job job)
    {
        if (job.Jobs.Count > 0 && job.Steps.Count > 0)
            throw new Exception("A job with subjobs cannot have steps");
    }
}