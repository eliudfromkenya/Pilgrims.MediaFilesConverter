using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Pilgrims.MediaFilesConverter.Services.UtilityUpgrade
{
    /// <summary>
    /// Configuration service for managing utility paths and settings
    /// </summary>
    public interface IUtilityConfigurationService
    {
        /// <summary>
        /// Gets the configured path for a specific utility
        /// </summary>
        string GetUtilityPath(string utilityName);

        /// <summary>
        /// Sets the path for a specific utility
        /// </summary>
        Task SetUtilityPathAsync(string utilityName, string path);

        /// <summary>
        /// Gets the default download directory for utilities
        /// </summary>
        string GetDefaultDownloadDirectory();

        /// <summary>
        /// Gets the temporary directory for downloads and extractions
        /// </summary>
        string GetTempDirectory();

        /// <summary>
        /// Gets the configured timeout for operations
        /// </summary>
        TimeSpan GetOperationTimeout();

        /// <summary>
        /// Validates that a utility exists at the specified path
        /// </summary>
        Task<bool> ValidateUtilityPathAsync(string utilityName, string path);

        /// <summary>
        /// Gets the executable name for a utility based on the current platform
        /// </summary>
        string GetExecutableName(string utilityName);

        /// <summary>
        /// Resolves the full path to a utility executable
        /// </summary>
        Task<string> ResolveUtilityPathAsync(string utilityName);

        /// <summary>
        /// Clears the configuration cache
        /// </summary>
        void ClearCache();
    }

    /// <summary>
    /// Configuration service implementation for utility management
    /// </summary>
    public class UtilityConfigurationService : IUtilityConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UtilityConfigurationService> _logger;
        private readonly string _configFilePath;
        private readonly object _cacheLock = new object();
        private readonly Dictionary<string, string> _utilityPathCache = new();

        public UtilityConfigurationService(IConfiguration configuration, ILogger<UtilityConfigurationService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Determine config file path based on platform
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "PilgrimsMediaFilesConverter");
            _configFilePath = Path.Combine(appFolder, "utility-config.json");
            
            // Ensure app folder exists
            Directory.CreateDirectory(appFolder);
            
            LoadConfiguration();
        }

        /// <summary>
        /// Gets the configured path for a specific utility
        /// </summary>
        public string GetUtilityPath(string utilityName)
        {
            if (string.IsNullOrWhiteSpace(utilityName))
                throw new ArgumentException("Utility name cannot be null or empty", nameof(utilityName));

            lock (_cacheLock)
            {
                if (_utilityPathCache.TryGetValue(utilityName, out var cachedPath))
                {
                    return cachedPath;
                }
            }

            // Try to get from configuration
            var configPath = _configuration[$"Utilities:{utilityName}:Path"];
            
            if (!string.IsNullOrEmpty(configPath))
            {
                lock (_cacheLock)
                {
                    _utilityPathCache[utilityName] = configPath;
                }
                return configPath;
            }

            // Return default path if not configured
            return GetDefaultUtilityPath(utilityName);
        }

        /// <summary>
        /// Sets the path for a specific utility
        /// </summary>
        public async Task SetUtilityPathAsync(string utilityName, string path)
        {
            if (string.IsNullOrWhiteSpace(utilityName))
                throw new ArgumentException("Utility name cannot be null or empty", nameof(utilityName));

            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            try
            {
                // Validate the path
                if (!await ValidateUtilityPathAsync(utilityName, path))
                {
                    throw new InvalidOperationException($"Invalid utility path: {path}");
                }

                // Update cache
                lock (_cacheLock)
                {
                    _utilityPathCache[utilityName] = path;
                }

                // Save to configuration file
                await SaveConfigurationAsync();
                
                _logger.LogInformation("Updated utility path for {UtilityName}: {Path}", utilityName, path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting utility path for {UtilityName}", utilityName);
                throw;
            }
        }

        /// <summary>
        /// Gets the default download directory for utilities
        /// </summary>
        public string GetDefaultDownloadDirectory()
        {
            var configuredPath = _configuration["Utilities:DownloadDirectory"];
            
            if (!string.IsNullOrEmpty(configuredPath))
            {
                return Path.GetFullPath(configuredPath);
            }

            // Default to user's downloads folder
            var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            return Path.Combine(downloadsPath, "PilgrimsMediaUtilities");
        }

        /// <summary>
        /// Gets the temporary directory for downloads and extractions
        /// </summary>
        public string GetTempDirectory()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "PilgrimsMediaConverter");
            Directory.CreateDirectory(tempPath);
            return tempPath;
        }

        /// <summary>
        /// Gets the configured timeout for operations
        /// </summary>
        public TimeSpan GetOperationTimeout()
        {
            var timeoutMinutes = _configuration.GetValue("Utilities:OperationTimeoutMinutes", 30);
            return TimeSpan.FromMinutes(timeoutMinutes);
        }

        /// <summary>
        /// Validates that a utility exists at the specified path
        /// </summary>
        public async Task<bool> ValidateUtilityPathAsync(string utilityName, string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return false;

                // Check if file exists
                if (!File.Exists(path))
                    return false;

                // Check if it's executable (basic validation)
                var fileInfo = new FileInfo(path);
                if (fileInfo.Length == 0)
                    return false;

                // For Windows, check if it has .exe extension or is a known executable
                if (OperatingSystem.IsWindows())
                {
                    var extension = Path.GetExtension(path).ToLowerInvariant();
                    if (extension != ".exe" && extension != ".com" && extension != ".bat" && extension != ".cmd")
                    {
                        // Check if it's a known utility name without extension
                        var fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                        if (fileName != utilityName.ToLowerInvariant())
                        {
                            return false;
                        }
                    }
                }

                // Try to execute with version command to validate it's working
                try
                {
                    var executableName = GetExecutableName(utilityName);
                    if (Path.GetFileName(path).Equals(executableName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Basic test - try to run with --version or similar
                        var testResult = await TestUtilityExecutionAsync(path, utilityName);
                        return testResult;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to test utility execution for {UtilityName} at {Path}", utilityName, path);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating utility path for {UtilityName}: {Path}", utilityName, path);
                return false;
            }
        }

        /// <summary>
        /// Gets the executable name for a utility based on the current platform
        /// </summary>
        public string GetExecutableName(string utilityName)
        {
            if (string.IsNullOrWhiteSpace(utilityName))
                throw new ArgumentException("Utility name cannot be null or empty", nameof(utilityName));

            var baseName = utilityName.ToLowerInvariant();
            
            if (OperatingSystem.IsWindows())
            {
                return baseName switch
                {
                    "ffmpeg" => "ffmpeg.exe",
                    "ffprobe" => "ffprobe.exe",
                    "yt-dlp" => "yt-dlp.exe",
                    "youtube-dl" => "youtube-dl.exe",
                    _ => $"{baseName}.exe"
                };
            }
            else
            {
                return baseName switch
                {
                    "ffmpeg" => "ffmpeg",
                    "ffprobe" => "ffprobe",
                    "yt-dlp" => "yt-dlp",
                    "youtube-dl" => "youtube-dl",
                    _ => baseName
                };
            }
        }

        /// <summary>
        /// Resolves the full path to a utility executable
        /// </summary>
        public async Task<string> ResolveUtilityPathAsync(string utilityName)
        {
            try
            {
                // First check configured path
                var configuredPath = GetUtilityPath(utilityName);
                
                if (await ValidateUtilityPathAsync(utilityName, configuredPath))
                {
                    return configuredPath;
                }

                // Check in system PATH
                var executableName = GetExecutableName(utilityName);
                var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                var paths = pathVariable.Split(Path.PathSeparator);

                foreach (var path in paths)
                {
                    var fullPath = Path.Combine(path, executableName);
                    if (await ValidateUtilityPathAsync(utilityName, fullPath))
                    {
                        return fullPath;
                    }
                }

                // Check in default locations
                var defaultPath = GetDefaultUtilityPath(utilityName);
                if (await ValidateUtilityPathAsync(utilityName, defaultPath))
                {
                    return defaultPath;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving utility path for {UtilityName}", utilityName);
                return string.Empty;
            }
        }

        /// <summary>
        /// Clears the configuration cache
        /// </summary>
        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _utilityPathCache.Clear();
            }
            LoadConfiguration();
        }

        private string GetDefaultUtilityPath(string utilityName)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "PilgrimsMediaFilesConverter", "Utilities");
            var executableName = GetExecutableName(utilityName);
            return Path.Combine(appFolder, utilityName, executableName);
        }

        private async Task<bool> TestUtilityExecutionAsync(string path, string utilityName)
        {
            try
            {
                using var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = path;
                process.StartInfo.Arguments = "--version";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                await process.WaitForExitAsync();

                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    
                    if (config != null)
                    {
                        lock (_cacheLock)
                        {
                            foreach (var kvp in config)
                            {
                                _utilityPathCache[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading utility configuration from {ConfigFilePath}", _configFilePath);
            }
        }

        private async Task SaveConfigurationAsync()
        {
            try
            {
                Dictionary<string, string> config;
                lock (_cacheLock)
                {
                    config = new Dictionary<string, string>(_utilityPathCache);
                }

                var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(_configFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving utility configuration to {ConfigFilePath}", _configFilePath);
                throw;
            }
        }
    }
}