using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DotRun.Runtime
{

    public class LocalNode : Node
    {

        public override async Task WriteFile(StepContext context, string path, Stream source)
        {
            if (File.Exists(path))
                File.Delete(path);

            using var fileStream = File.Create(path);
            source.Seek(0, SeekOrigin.Begin);
            await source.CopyToAsync(fileStream);
        }

        public override Task<ProcessResult> ExecuteCommand(NodeCommand cmd)
        {
            return ExecuteLocalCommand(cmd);
        }

    }

}
