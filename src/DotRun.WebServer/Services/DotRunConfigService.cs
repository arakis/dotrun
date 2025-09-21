using System;
using System.IO;
using DotRun.Runtime.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DotRun.WebServer.Services;

public interface IDotRunConfigService
{
    DotRunConfig GetConfiguration();
}

public class DotRunConfigService : IDotRunConfigService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DotRunConfigService> _logger;
    private DotRunConfig _cachedConfig;
    private readonly object _lock = new object();

    public DotRunConfigService(IConfiguration configuration, ILogger<DotRunConfigService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public DotRunConfig GetConfiguration()
    {
        lock (_lock)
        {
            if (_cachedConfig != null)
                return _cachedConfig;

            var configPath = _configuration["DotRunConfigPath"];
            
            if (string.IsNullOrEmpty(configPath))
            {
                configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "dotrun.config",
                    "config-v2.yaml"
                );
                _logger.LogInformation("No DotRunConfigPath specified, using default: {ConfigPath}", configPath);
            }

            configPath = Environment.ExpandEnvironmentVariables(configPath);
            configPath = configPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            
            _logger.LogInformation("Loading DotRun configuration from: {ConfigPath}", configPath);
            
            if (!File.Exists(configPath))
            {
                _logger.LogWarning("Configuration file not found at {ConfigPath}, using empty configuration", configPath);
                _cachedConfig = new DotRunConfig();
                return _cachedConfig;
            }

            try
            {
                _cachedConfig = DotRunConfig.FromFile(configPath);
                _logger.LogInformation("Successfully loaded DotRun configuration with {Count} repositories", 
                    _cachedConfig.Repositories?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration from {ConfigPath}", configPath);
                _cachedConfig = new DotRunConfig();
            }

            return _cachedConfig;
        }
    }
}