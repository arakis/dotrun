using System;
using DotRun.Runtime;

namespace DotRun.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Hello World!");
            DotRunConfig.Current.Projects[0].Workflows[0].Run();
        }
    }
}
