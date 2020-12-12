// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DotRun.Runtime
{
    public abstract class Node : INode
    {

        public WorkflowContext Context { get; private set; }
        public IOutput InternalOutput => Context.InternalOutput;
        public NodeModel Model { get; private set; }

        public Node(WorkflowContext context, NodeModel model)
        {
            Context = context;
            Model = model;
        }

        public static async Task<INode> CreateNode(WorkflowContext context, NodeModel model)
        {
            INode node;
            switch (model.Type)
            {
                case NodeType.Local:
                    node = new LocalNode(context, model);
                    break;
                case NodeType.Docker:
                    node = new DockerNode(context, model);
                    break;
                default:
                    throw new Exception("Invalid node type");
            }

            if (!(await node.Init()))
                throw new Exception("Error");

            return node;
        }

        public virtual IShell CreateShell(string name)
        {
            if (string.IsNullOrEmpty(name))
                name = "cmd";

            switch (name)
            {
                case "cmd":
                    return new CmdShell();
                case "ps":
                    return new PowerShell();
                case "sh":
                    return new ShShell();
            }
            throw new Exception();
        }

        public abstract RunningProcess ExecuteCommand(NodeCommand cmd);
        public abstract Task WriteFile(StepContext context, string path, Stream source);

        protected RunningProcess ExecuteLocalCommand(NodeCommand cmd)
        {
            var result = new RunningProcess();
            TaskCompletionSource<bool> startedTask = new TaskCompletionSource<bool>();
            TaskCompletionSource startedOutput = new TaskCompletionSource();
            result.CancellationTokenSource = new CancellationTokenSource();
            result.CompletedTask = ExecuteLocalCommandInternal(cmd, startedTask, startedOutput, result.CancellationTokenSource.Token);
            result.StartedTask = startedTask.Task;
            result.StartedOutput = startedOutput.Task;
            return result;
        }

        private int ExecCount;

        private async Task<ProcessResult> ExecuteLocalCommandInternal(NodeCommand cmd, TaskCompletionSource<bool> startedTask, TaskCompletionSource startedOutput, CancellationToken cancellationToken)
        {
            var num = Interlocked.Increment(ref ExecCount);
            Console.WriteLine("LocalExec #" + num.ToString() + ":" + cmd.FileName + " " + string.Join(" ", cmd.Arguments));
            var result = new ProcessResult();

            var startInfo = new ProcessStartInfo
            {
                FileName = cmd.FileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            foreach (var arg in cmd.Arguments)
                startInfo.ArgumentList.Add(arg);

            var process = new Process()
            {
                StartInfo = startInfo,
            };

            var outputCloseEvent = new TaskCompletionSource<bool>();

            process.OutputDataReceived += (s, e) =>
            {
                lock (startedOutput)
                    if (!startedOutput.Task.IsCompleted)
                        startedOutput.SetResult();

                // The output stream has been closed i.e. the process has terminated
                if (e.Data == null)
                {
                    outputCloseEvent.SetResult(true);
                }
                else
                {
                    cmd.Output.WriteLine(e.Data);
                }
            };

            var errorCloseEvent = new TaskCompletionSource<bool>();

            process.ErrorDataReceived += (s, e) =>
            {
                lock (startedOutput)
                    if (!startedOutput.Task.IsCompleted)
                        startedOutput.SetResult();

                // The error stream has been closed i.e. the process has terminated
                if (e.Data == null)
                {
                    errorCloseEvent.SetResult(true);
                }
                else
                {
                    cmd.Output.ErrorLine(e.Data);
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

                startedTask.SetResult(true);

                // Creates task to wait for process exit using timeout
                var waitForExit = WaitForExitAsync(process, cancellationToken);

                // Create task to wait for process exit and closing all output streams
                var processTask = Task.WhenAll(waitForExit, outputCloseEvent.Task, errorCloseEvent.Task);

                // Waits process completion and then checks it was not completed by timeout
                if (await Task.WhenAny(Task.Delay(CapTimeout(cmd.Timeout)), processTask) == processTask && !waitForExit.IsCanceled)
                {
                    result.Completed = true;
                    result.ExitCode = process.ExitCode;
                    Console.WriteLine("LocalExec #" + num.ToString() + ": ExitCode " + result.ExitCode);
                }
                else
                {
                    try
                    {
                        // Kill hung process
                        process.Kill();
                        Console.WriteLine("LocalExec #" + num.ToString() + ": Killed");
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                startedTask.SetResult(false);
                Console.WriteLine("LocalExec #" + num.ToString() + ": Failed");
            }

            return result;
        }

        private static Task WaitForExitAsync(Process process, CancellationToken token)
        {
            return process.WaitForExitAsync(token);
            //return Task.Run(() => process.WaitForExitAsync((int)CapTimeout(timeout).TotalMilliseconds));
        }

        private static TimeSpan CapTimeout(TimeSpan ts)
        {
            if (ts.TotalMilliseconds > int.MaxValue || ts == TimeSpan.Zero)
                return TimeSpan.FromMilliseconds(int.MaxValue);
            return ts;
        }

        public virtual Task<bool> Init()
        {
            return Task.FromResult(true);
        }

        public virtual void Dispose()
        {
        }

        public virtual async ValueTask DisposeAsync()
        {
            await Task.Run(Dispose);
        }

        public abstract Task<string> FindExecutablePath(string executable);
        public abstract Task<string> GetHomeDir();
    }

}
