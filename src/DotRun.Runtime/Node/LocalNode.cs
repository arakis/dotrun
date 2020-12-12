using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotRun.Runtime
{

    public class LocalNode : Node
    {

        public LocalNode(WorkflowContext context, NodeModel model)
            : base(context, model)
        {
        }

        public override async Task WriteFile(StepContext context, string path, Stream source)
        {
            if (File.Exists(path))
                File.Delete(path);

            using var fileStream = File.Create(path);
            source.Seek(0, SeekOrigin.Begin);
            await source.CopyToAsync(fileStream);
        }

        public override RunningProcess ExecuteCommand(NodeCommand cmd)
        {
            return ExecuteLocalCommand(cmd);
        }

        public override Task<string> FindExecutablePath(string executable)
        {
            throw new NotImplementedException();
        }

        public override Task<string> GetHomeDir()
        {
            return Task.FromResult(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }
    }

}
