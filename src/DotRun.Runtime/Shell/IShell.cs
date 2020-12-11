using System.Threading.Tasks;

namespace DotRun.Runtime
{
    public interface IShell
    {

        Task<StepResult> Execute(StepContext context, IOutput output);

    }

}
