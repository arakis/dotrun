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

            var cfgFile = "~/dotrun.config/config.yaml";

            var idx = Array.IndexOf(args, "--config-file");
            if (idx > 0 && idx < args.Length)
                cfgFile = args[idx + 1];

            var cfg = DotRunConfig.FromFile(cfgFile);

            await cfg.Projects[0].Workflows[0].Run();
            System.Console.WriteLine("Done");
        }
    }
}
