using System;
using System.Threading.Tasks;
using DotRun.Runtime.Tests;

namespace DotRun.Tests.Runner
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            await new UnitTest1().Docker();
        }
    }

}
