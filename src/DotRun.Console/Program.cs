using System;
using DotRun.Runtime;

namespace DotRun.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Hello World!");
            var r = DotRunConfig.Current.Projects[0].Workflows[0].Run().Result;
            System.Console.WriteLine("Done");
        }
    }
}
