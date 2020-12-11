﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;
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


        public async Task<ProcessResult> ExecuteCommand(ShellCommand cmd)
        {
            var result = new ProcessResult();

            var startInfo = new ProcessStartInfo
            {
                FileName = cmd.proc,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            foreach (var arg in cmd.args)
                startInfo.ArgumentList.Add(arg);

            var process = new Process()
            {
                StartInfo = startInfo,
            };

            var outputCloseEvent = new TaskCompletionSource<bool>();

            process.OutputDataReceived += (s, e) =>
            {
                // The output stream has been closed i.e. the process has terminated
                if (e.Data == null)
                {
                    outputCloseEvent.SetResult(true);
                }
                else
                {
                    cmd.output.WriteLine(e.Data);
                }
            };

            var errorCloseEvent = new TaskCompletionSource<bool>();

            process.ErrorDataReceived += (s, e) =>
            {
                // The error stream has been closed i.e. the process has terminated
                if (e.Data == null)
                {
                    errorCloseEvent.SetResult(true);
                }
                else
                {
                    cmd.output.ErrorLine(e.Data);
                }
            };

            bool isStarted;

            try
            {
                isStarted = process.Start();
            }
            catch
            {
                // Usually it occurs when an executable file is not found or is not executable

                result.Completed = true;
                result.ExitCode = -1;

                isStarted = false;
            }

            if (isStarted)
            {
                // Reads the output stream first and then waits because deadlocks are possible
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Creates task to wait for process exit using timeout
                var waitForExit = WaitForExitAsync(process, cmd.timeout);

                // Create task to wait for process exit and closing all output streams
                var processTask = Task.WhenAll(waitForExit, outputCloseEvent.Task, errorCloseEvent.Task);

                // Waits process completion and then checks it was not completed by timeout
                if (await Task.WhenAny(Task.Delay(CapTimeout(cmd.timeout)), processTask) == processTask && waitForExit.Result)
                {
                    result.Completed = true;
                    result.ExitCode = process.ExitCode;
                }
                else
                {
                    try
                    {
                        // Kill hung process
                        process.Kill();
                    }
                    catch
                    {
                    }
                }
            }

            return result;
        }

        private static Task<bool> WaitForExitAsync(Process process, TimeSpan timeout)
        {
            return Task.Run(() => process.WaitForExit((int)CapTimeout(timeout).TotalMilliseconds));
        }

        private static TimeSpan CapTimeout(TimeSpan ts)
        {
            if (ts.TotalMilliseconds > int.MaxValue)
                return TimeSpan.FromMilliseconds(int.MaxValue);
            return ts;
        }

        public IShell CreateShell(string name)
        {
            if (string.IsNullOrEmpty(name))
                name = "cmd";

            switch (name)
            {
                case "cmd":
                    return new CmdShell();
                case "ps":
                    return new PowerShell();
            }
            throw new Exception();
        }
    }

}
