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

        public static Task<StepExecutionResult> Run(this Step step, StepExecutionContext context)
        {
            return StepExecutor.Create(step, context).Run();
        }

    }

    public class StepExecutor : IStepExecutor
    {

        public Step Step { get; set; }
        public StepExecutionContext Context { get; set; }

        public IStepEnvironment Environment { get; set; }

        public IStepShell Shell { get; set; }

        public static IStepExecutor Create(Step step, StepExecutionContext context)
        {
            var executor = new StepExecutor(step, context);
            executor.Environment = context.ExecutionContext.GetEnvironment(step.Environment);
            executor.Shell = new CmdShell();
            return executor;
        }

        private StepExecutor(Step step, StepExecutionContext context)
        {
            Step = step;
            Context = context;
        }

        public Task<StepExecutionResult> Run()
        {
            var result = new StepExecutionResult();
            return Task.FromResult(result);
        }

        public Task<StepExecutionResult> RunInternal()
        {
            var result = new StepExecutionResult();
            return Task.FromResult(result);
        }

        public void Abort()
        {
            throw new System.NotImplementedException();
        }
    }

    public interface IStepExecutor
    {
        public Step Step { get; }
        public StepExecutionContext Context { get; }
        public IStepEnvironment Environment { get; }
        public IStepShell Shell { get; }
        public void Abort();
        public Task<StepExecutionResult> Run();
    }

    public interface IStepEnvironment
    {
        Task WriteFile(StepExecutionContext context, string path, Stream source);

        Task ExecuteCommand(StepExecutionContext context, string proc, IEnumerable<string> args);

    }

    public interface IStepShell
    {

        Task Execute(StepExecutionContext context);

    }

    public class LocalEnvironment : IStepEnvironment
    {
        public Task WriteFile(StepExecutionContext context, string path, Stream source)
        {
            using var fileStream = File.Create(path);
            source.Seek(0, SeekOrigin.Begin);
            return source.CopyToAsync(fileStream);
        }

        public Task ExecuteCommand(StepExecutionContext context, string proc, IEnumerable<string> args)
        {
            Process.Start(proc, args);
            return Task.CompletedTask;
        }
    }

    public class CmdShell : IStepShell
    {

        public Task Execute(StepExecutionContext context)
        {
            var command = context.Step.Run;
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(command));
            var tmpScriptPath = "/tmp/script.bat";
            context.Environment.WriteFile(context, tmpScriptPath, ms);
            return context.Environment.ExecuteCommand(context, "cmd.exe", new string[] { "/c", tmpScriptPath });
        }

    }

}
