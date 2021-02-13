using System;
using System.IO;
using Xunit;

namespace DotRun.Runtime.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async void Test1()
        {
            var binDir = Path.GetDirectoryName(new FileInfo(typeof(UnitTest1).Assembly.Location).FullName);
            var cfgFile = Path.Combine(binDir, "Configuration", "config.yaml");
            var cfg = DotRunConfig.FromFile(cfgFile);
            await cfg.Projects["proj1"].Workflows["demo"].Run();
        }
    }
}
