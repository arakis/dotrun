// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DotRun.Runtime
{

    public class GitCloneUseAction : UseAction
    {
        public GitCloneUseAction(StepContext context) : base(context)
        {
        }

        public override async Task Run()
        {
            var repo = (string)Step.With["repository"];

            if (Node.Platform.PlatformType == PlatformType.Unix)
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

                // remove key
                await Node.ExecuteCommand(new NodeCommand { FileName = "/bin/sh", Arguments = new string[] { "-c", "'rm ~/.ssh/id_rsa_dotrun.tmp'" } }).CompletedTask;
            }
            else
            {

                await Node.ExecuteCommand(new NodeCommand
                {
                    FileName = "git",
                    Arguments = new string[] { "clone", repo },
                    Output = new ConsoleOutput(),
                }).CompletedTask;

            }
        }
    }

}
