using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DotRun.Runtime
{
    public class LocalNode : INode
    {
        public async Task WriteFile(StepContext context, string path, Stream source)
        {
            if (File.Exists(path))
                File.Delete(path);

            using var fileStream = File.Create(path);
            source.Seek(0, SeekOrigin.Begin);
            await source.CopyToAsync(fileStream);
        }

        public Task ExecuteCommand(StepContext context, string proc, IEnumerable<string> args, IOutput output)
        {
            Process.Start(proc, args);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = proc,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                foreach (var arg in args)
                    startInfo.ArgumentList.Add(arg);

                var process = Process.Start(startInfo);

                while (!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();
                    output.WriteLine(line);
                }

                process.WaitForExit();
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
            }
            return Task.CompletedTask;
        }

        public IShell CreateShell(string name)
        {
            if (string.IsNullOrEmpty(name))
                name = "cmd";
            switch (name)
            {
                case "cmd":
                    return new CmdShell();
            }
            throw new Exception();
        }
    }

}
