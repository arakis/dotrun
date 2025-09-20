using System.IO;
using Microsoft.AspNetCore.Mvc;
using DotRun.WebServer.Services;

namespace DotRun.WebServer.Controllers;

public class SettingsController : Controller
{
    private readonly IDotRunConfigService _configService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        IDotRunConfigService configService,
        IConfiguration configuration,
        ILogger<SettingsController> logger)
    {
        _configService = configService;
        _configuration = configuration;
        _logger = logger;
    }

    public IActionResult Index()
    {
        var configPath = _configuration["DotRunConfigPath"];
        
        if (string.IsNullOrEmpty(configPath))
        {
            configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "dotrun.config",
                "config.yaml"
            );
        }
        
        configPath = Environment.ExpandEnvironmentVariables(configPath);
        configPath = configPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        
        string yamlContent = "";
        
        if (System.IO.File.Exists(configPath))
        {
            try
            {
                yamlContent = System.IO.File.ReadAllText(configPath);
                _logger.LogInformation("Successfully loaded YAML configuration from {ConfigPath}", configPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read YAML configuration from {ConfigPath}", configPath);
                yamlContent = $"# Error loading configuration file\n# {ex.Message}";
            }
        }
        else
        {
            yamlContent = "# Configuration file not found\n# Expected location: " + configPath;
        }
        
        ViewBag.ConfigPath = configPath;
        ViewBag.YamlContent = yamlContent;
        
        return View();
    }
}