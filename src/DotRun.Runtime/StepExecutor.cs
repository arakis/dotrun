// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DotRun.Runtime
{
    public class StepExecutor : IStepExecutor
    {

        public Step Step { get; private set; }
        public StepContext Context { get; private set; }

        public INode Node { get; private set; }

        public IShell Shell { get; private set; }
        public IOutput Output { get; private set; }

        public static IStepExecutor Create(Step step, StepContext context)
        {
            var executor = new StepExecutor(step, context);
            executor.Node = context.WorkflowContext.GetNode(step.Node);
            executor.Shell = context.Node.CreateShell(step.Shell);
            executor.Output = new ConsoleOutput();
            return executor;
        }

        private StepExecutor(Step step, StepContext context)
        {
            Step = step;
            Context = context;
        }

        public Task<StepResult> Run()
        {
            return RunInternal();
        }

        internal async Task<StepResult> RunInternal()
        {
            if (!string.IsNullOrEmpty(Step.Uses))
                await RunUses();

            if (!string.IsNullOrEmpty(Step.Run))
                return await Shell.Execute(Context, Output);

            return new StepResult();
        }

        private async Task RunUses()
        {
            if (Step.Uses == "dotrun/checkout")
            {
                var updated = false;
                var gitPath = await Node.FindExecutablePath("git");
                if (string.IsNullOrEmpty(gitPath))
                {
                    if (!updated)
                    {
                        await Node.ExecuteCommand(new NodeCommand { FileName = "apt-get", Arguments = new string[] { "update" } }).CompletedTask;
                        updated = true;
                    }
                    await Node.ExecuteCommand(new NodeCommand { FileName = "apt-get", Arguments = new string[] { "install", "-y", "git" } }).CompletedTask;
                }

                var sshPath = await Node.FindExecutablePath("ssh");
                if (string.IsNullOrEmpty(sshPath))
                {
                    if (!updated)
                    {
                        await Node.ExecuteCommand(new NodeCommand { FileName = "apt-get", Arguments = new string[] { "update" } }).CompletedTask;
                        updated = true;
                    }
                    await Node.ExecuteCommand(new NodeCommand { FileName = "apt-get", Arguments = new string[] { "install", "-y", "openssh-client" } }).CompletedTask;
                }

                var repo = (string)Step.With["repository"];
                await Node.ExecuteCommand(new NodeCommand { FileName = "mkdir", Arguments = new string[] { "-p", "~/.ssh" } }).CompletedTask;
                using var stream = File.Open(DirectoryHelper.GetAbsoluteLocalPath("~/.ssh/id_rsa"), FileMode.Open, FileAccess.Read, FileShare.Read);

                await Node.WriteFile(Context, "~/.ssh/id_rsa_dotrun.tmp", stream);
                await Node.ExecuteCommand(new NodeCommand { FileName = "/bin/sh", Arguments = new string[] { "-c", "'chmod 600 ~/.ssh/id_rsa_dotrun.tmp'" } }).CompletedTask;
                await Node.ExecuteCommand(new NodeCommand
                {
                    FileName = "git",
                    Arguments = new string[] { "clone", repo },
                    Env = new()
                    {
                        { "GIT_SSH_COMMAND", "ssh -i ~/.ssh/id_rsa_dotrun.tmp -o UserKnownHostsFile=/dev/null -o StrictHostKeyChecking=no" },
                    },
                }).CompletedTask;

                System.Console.WriteLine("Step.Uses done: " + Step.Uses);
            }

            return;
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }
    }

}
