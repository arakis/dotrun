using System.Threading.Tasks;

namespace DotRun.Runtime.Runners;

public interface IJobRunner
{
    Task<JobResult> RunAsync(Job job, JobContext jobContext);
}