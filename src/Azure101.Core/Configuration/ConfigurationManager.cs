using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Azure101.Core.Configuration;

/// <summary>
/// Centralized configuration management for Azure101
/// Supports appsettings.json, environment variables, and user secrets
/// </summary>
public class ConfigurationManager
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationManager> _logger;

    public ConfigurationManager(IConfiguration configuration, ILogger<ConfigurationManager> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Gets a required configuration value, throws if not found
    /// </summary>
    public string GetRequiredValue(string key)
    {
        var value = _configuration[key];
        if (string.IsNullOrEmpty(value))
        {
            _logger.LogError("Required configuration key '{Key}' not found", key);
            throw new InvalidOperationException($"Required configuration key '{key}' not found");
        }
        return value;
    }

    /// <summary>
    /// Gets a configuration value with a default fallback
    /// </summary>
    public string GetValue(string key, string defaultValue = "")
    {
        return _configuration[key] ?? defaultValue;
    }

    /// <summary>
    /// Gets a configuration value and parses it to the specified type
    /// </summary>
    public T? GetValue<T>(string key)
    {
        return _configuration.GetValue<T>(key);
    }

    /// <summary>
    /// Gets a configuration section and binds it to the specified type
    /// </summary>
    public T GetSection<T>(string sectionName) where T : new()
    {
        var section = new T();
        _configuration.GetSection(sectionName).Bind(section);
        return section;
    }

    /// <summary>
    /// Validates that all required keys are present in configuration
    /// </summary>
    public void ValidateRequiredKeys(params string[] keys)
    {
        var missingKeys = keys.Where(key => string.IsNullOrEmpty(_configuration[key])).ToList();
        if (missingKeys.Any())
        {
            var keyList = string.Join(", ", missingKeys);
            _logger.LogError("Missing required configuration keys: {Keys}", keyList);
            throw new InvalidOperationException($"Missing required configuration keys: {keyList}");
        }
        _logger.LogInformation("All required configuration keys validated successfully");
    }
}
