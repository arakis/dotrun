using System.Threading.Tasks;

namespace DotRun.Runtime.Services;

public interface IConfigurationLoader
{
    Task<DotRunConfigLegacy> LoadAsync(string configFilePath);
    Task<bool> ValidateAsync(DotRunConfigLegacy configLegacy);
}