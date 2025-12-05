using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Pilgrims.MediaFilesConverter.Services.UtilityUpgrade
{
    /// <summary>
    /// Base implementation of version checker for utilities
    /// </summary>
    public abstract class BaseVersionChecker : IVersionChecker
    {
        protected readonly ILogger<BaseVersionChecker> _logger;

        protected BaseVersionChecker(ILogger<BaseVersionChecker> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the current installed version of the utility
        /// </summary>
        public async Task<string?> GetCurrentVersionAsync(string executablePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                {
                    _logger.LogWarning("Executable path is invalid or file does not exist: {ExecutablePath}", executablePath);
                    return null;
                }

                return await ExecuteVersionCommandAsync(executablePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current version for utility at {ExecutablePath}", executablePath);
                return null;
            }
        }

        /// <summary>
        /// Gets the latest available version from the official source
        /// </summary>
        public abstract Task<string?> GetLatestVersionAsync();

        /// <summary>
        /// Compares two version strings
        /// </summary>
        public VersionComparisonResult CompareVersions(string currentVersion, string latestVersion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(currentVersion) || string.IsNullOrWhiteSpace(latestVersion))
                {
                    return VersionComparisonResult.ComparisonFailed;
                }

                var current = NormalizeVersion(currentVersion);
                var latest = NormalizeVersion(latestVersion);

                var currentVersionObj = ParseVersion(current);
                var latestVersionObj = ParseVersion(latest);

                if (currentVersionObj == null || latestVersionObj == null)
                {
                    return VersionComparisonResult.ComparisonFailed;
                }

                var comparison = currentVersionObj.CompareTo(latestVersionObj);
                
                return comparison switch
                {
                    < 0 => VersionComparisonResult.UpdateAvailable,
                    0 => VersionComparisonResult.UpToDate,
                    > 0 => VersionComparisonResult.NewerThanLatest
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing versions: current={CurrentVersion}, latest={LatestVersion}", currentVersion, latestVersion);
                return VersionComparisonResult.ComparisonFailed;
            }
        }

        /// <summary>
        /// Executes the version command for the utility
        /// </summary>
        protected virtual async Task<string?> ExecuteVersionCommandAsync(string executablePath)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning("Version command failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
                    return null;
                }

                return ExtractVersionFromOutput(output);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing version command for {ExecutablePath}", executablePath);
                return null;
            }
        }

        /// <summary>
        /// Extracts version from command output
        /// </summary>
        protected virtual string? ExtractVersionFromOutput(string output)
        {
            // Default implementation - extract version pattern from output
            var versionPattern = @"\b(\d+\.\d+(?:\.\d+)?(?:\.\d+)?(?:-\w+)?)\b";
            var match = Regex.Match(output, versionPattern);
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// Normalizes version string for comparison
        /// </summary>
        protected virtual string NormalizeVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return version;

            // Remove leading 'v' or 'version' text
            version = Regex.Replace(version, @"^[vV]", "");
            version = Regex.Replace(version, @"^version\s*", "", RegexOptions.IgnoreCase);
            
            return version.Trim();
        }

        /// <summary>
        /// Parses version string to Version object
        /// </summary>
        private Version? ParseVersion(string versionString)
        {
            try
            {
                // Handle pre-release versions by removing pre-release suffix
                var cleanVersion = Regex.Replace(versionString, @"-\w+$", "");
                
                // Handle versions with more than 4 parts by truncating
                var parts = cleanVersion.Split('.');
                if (parts.Length > 4)
                {
                    cleanVersion = string.Join(".", parts.Take(4));
                }
                
                return Version.TryParse(cleanVersion, out var version) ? version : null;
            }
            catch
            {
                return null;
            }
        }
    }
}