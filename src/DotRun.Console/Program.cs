using System;
using System.Threading.Tasks;
using DotRun.Runtime;

namespace DotRun.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            System.Console.WriteLine("Init DotRun");
            await DotRunConfig.Current.Projects[0].Workflows[0].Run();
            System.Console.WriteLine("Done");
        }
    }
}
