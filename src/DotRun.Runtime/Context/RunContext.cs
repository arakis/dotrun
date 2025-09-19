using System;
using Microsoft.Extensions.DependencyInjection;

namespace DotRun.Runtime;

public class RunContext
{
    public IServiceProvider ServiceProvider { get; set; }
    public IServiceScope ServiceScope { get; set; }
}