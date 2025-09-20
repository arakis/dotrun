using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotRun.Runtime.Services;

public class ConfigurationLoader : IConfigurationLoader
{
    private readonly ILogger<ConfigurationLoader> _logger;

    public ConfigurationLoader(ILogger<ConfigurationLoader> logger)
    {
        _logger = logger;
    }

    public Task<DotRunConfig> LoadAsync(string configFilePath)
    {
        try
        {
            _logger.LogDebug("Loading configuration from {ConfigFile}", configFilePath);

            if (string.IsNullOrWhiteSpace(configFilePath))
            {
                throw new ArgumentNullException(nameof(configFilePath), "Configuration file path cannot be null or empty");
            }

            // Expand paths with ~ to the absolute path
            configFilePath = DirectoryHelper.GetAbsoluteLocalPath(configFilePath);

            if (!File.Exists(configFilePath))
            {
                throw new FileNotFoundException($"Configuration file not found: {configFilePath}", configFilePath);
            }

            var config = DotRunConfig.FromFile(configFilePath);

            _logger.LogInformation("Configuration loaded successfully from {ConfigFile}", configFilePath);
            _logger.LogDebug("Projects found: {ProjectCount}", config.Projects?.Count ?? 0);

            return Task.FromResult(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from {ConfigFile}", configFilePath);
            throw;
        }
    }

    public Task<bool> ValidateAsync(DotRunConfig config)
    {
        if (config == null)
        {
            _logger.LogError("Configuration is null");
            return Task.FromResult(false);
        }

        bool isValid = true;

        if (string.IsNullOrWhiteSpace(config.ConfigDirectory))
        {
            _logger.LogError("Configuration directory is not set");
            isValid = false;
        }

        if (!Directory.Exists(config.ProjectsRootDirectory))
        {
            _logger.LogWarning("Projects root directory does not exist: {Directory}", config.ProjectsRootDirectory);
        }

        if (config.Projects == null || config.Projects.Count == 0)
        {
            _logger.LogWarning("No projects found in configuration");
        }

        _logger.LogDebug("Configuration validation result: {IsValid}", isValid);
        return Task.FromResult(isValid);
    }
}