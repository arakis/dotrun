using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DotRun.Runtime
{

    public static class StepExtensions
    {

        public static Task<StepResult> Run(this Step step, StepContext context)
        {
            return StepExecutor.Create(step, context).Run();
        }

    }

    public class StepExecutor : IStepExecutor
    {

        public Step Step { get; private set; }
        public StepContext Context { get; private set; }

        public IStepEnvironment Environment { get; private set; }

        public IStepShell Shell { get; private set; }
        public IOutput Output { get; private set; }

        public static IStepExecutor Create(Step step, StepContext context)
        {
            var executor = new StepExecutor(step, context);
            executor.Environment = context.WorkflowContext.GetEnvironment(step.Environment);
            executor.Shell = context.Environment.CreateShell(step.Shell);
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

        internal Task<StepResult> RunInternal()
        {
            return Shell.Execute(Context, Output);
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }
    }

    public interface IStepExecutor
    {
        public Step Step { get; }
        public StepContext Context { get; }
        public IStepEnvironment Environment { get; }
        public IStepShell Shell { get; }
        public IOutput Output { get; }
        public void Abort();
        public Task<StepResult> Run();
    }

    public interface IStepEnvironment
    {
        Task WriteFile(StepContext context, string path, Stream source);

        Task ExecuteCommand(StepContext context, string proc, IEnumerable<string> args, IOutput output);

        public IStepShell CreateShell(string name);
    }

    public interface IOutput
    {
        void Write(string text);
        void WriteLine(string text);
    }

    public class NullOutput : IOutput
    {
        public void Write(string text)
        {
        }

        public void WriteLine(string text)
        {
        }
    }

    public class ConsoleOutput : IOutput
    {
        public void Write(string text)
        {
            Console.Write(text);
        }

        public void WriteLine(string text)
        {
            Console.Write("> ");
            Console.WriteLine(text);
        }
    }

    public interface IStepShell
    {

        Task<StepResult> Execute(StepContext context, IOutput output);

    }

    public class LocalEnvironment : IStepEnvironment
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

        public IStepShell CreateShell(string name)
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

    public class CmdShell : IStepShell
    {

        public async Task<StepResult> Execute(StepContext context, IOutput output)
        {
            var command = context.Step.Run;
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(command));
            var tmpScriptPath = "/tmp/script.bat";
            await context.Environment.WriteFile(context, tmpScriptPath, ms);
            await context.Environment.ExecuteCommand(context, "cmd.exe", new string[] { "/c", tmpScriptPath }, output);
            return new StepResult();
        }

    }

}
