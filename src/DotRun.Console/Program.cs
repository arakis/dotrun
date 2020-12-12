using System;
using System.Threading.Tasks;
using DotRun.Runtime;
using System.Linq;

namespace DotRun.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            System.Console.WriteLine("Init DotRun");

            string cfgFile = null;

            var idx = Array.IndexOf(args, "--config-file");
            if (idx > -1 && idx + 1 < args.Length)
                cfgFile = args[idx + 1];

            if (cfgFile == null)
            {
                System.Console.WriteLine("Please specify --config-file");
                return;
            }

            var cfg = DotRunConfig.FromFile(cfgFile);

            await cfg.Projects[0].Workflows[0].Run();
            System.Console.WriteLine("Done");
        }
    }
}
