using System.Threading.Tasks;

namespace DotRun.Runtime.Services;

public interface IConfigurationLoader
{
    Task<DotRunConfig> LoadAsync(string configFilePath);
    Task<bool> ValidateAsync(DotRunConfig config);
}