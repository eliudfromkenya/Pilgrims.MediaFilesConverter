using System;
using System.Threading.Tasks;

namespace Pilgrims.MediaFilesConverter.Services.UtilityUpgrade
{
    /// <summary>
    /// Interface for version checking services
    /// </summary>
    public interface IVersionChecker
    {
        /// <summary>
        /// Gets the current installed version of the utility
        /// </summary>
        Task<string?> GetCurrentVersionAsync(string executablePath);

        /// <summary>
        /// Gets the latest available version from the official source
        /// </summary>
        Task<string?> GetLatestVersionAsync();

        /// <summary>
        /// Compares two version strings
        /// </summary>
        VersionComparisonResult CompareVersions(string currentVersion, string latestVersion);
    }

    /// <summary>
    /// Result of version comparison
    /// </summary>
    public enum VersionComparisonResult
    {
        /// <summary>
        /// Current version is up to date
        /// </summary>
        UpToDate,

        /// <summary>
        /// Update is available
        /// </summary>
        UpdateAvailable,

        /// <summary>
        /// Current version is newer than latest (pre-release/beta)
        /// </summary>
        NewerThanLatest,

        /// <summary>
        /// Version comparison failed
        /// </summary>
        ComparisonFailed
    }
}