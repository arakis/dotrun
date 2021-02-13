using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace DotRun.Runtime.Tests
{
    public class UnitTest1
    {
        private async Task<WorkflowResult> Run(string project, string job)
        {
            var binDir = Path.GetDirectoryName(new FileInfo(typeof(UnitTest1).Assembly.Location).FullName);
            var cfgFile = Path.Combine(binDir, "Configuration", "config.yaml");
            var cfg = DotRunConfig.FromFile(cfgFile);
            return await cfg.Projects[project].Workflows[job].Run();
        }

        [Fact]
        public async void Test1()
        {
            var result = await Run("proj1", "demo");
            Assert.False(result.Failed);
        }

        [Fact]
        public async void Error()
        {
            var result = await Run("proj1", "error");
            Assert.True(result.Failed);
        }
    }
}
