using System;

namespace Pilgrims.MediaFilesConverter.Models
{
    /// <summary>
    /// Represents information about a utility (FFmpeg or yt-dlp)
    /// </summary>
    public class UtilityInfo
    {
        /// <summary>
        /// Name of the utility
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Current installed version
        /// </summary>
        public string? CurrentVersion { get; set; }

        /// <summary>
        /// Latest available version
        /// </summary>
        public string? LatestVersion { get; set; }

        /// <summary>
        /// Path to the utility executable
        /// </summary>
        public string? ExecutablePath { get; set; }

        /// <summary>
        /// Whether the utility is currently available
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Whether an update is available
        /// </summary>
        public bool IsUpdateAvailable => !string.IsNullOrEmpty(LatestVersion) && 
                                       !string.IsNullOrEmpty(CurrentVersion) && 
                                       IsNewerVersion(LatestVersion, CurrentVersion);

        /// <summary>
        /// Status message for the utility
        /// </summary>
        public string? StatusMessage { get; set; }

        /// <summary>
        /// Download URL for the utility
        /// </summary>
        public string? DownloadUrl { get; set; }

        /// <summary>
        /// Download size in bytes
        /// </summary>
        public long? DownloadSize { get; set; }

        /// <summary>
        /// Error message if utility check failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Suggested action to resolve issues
        /// </summary>
        public string? SuggestedAction { get; set; }

        /// <summary>
        /// Compares two version strings to determine if one is newer
        /// </summary>
        private static bool IsNewerVersion(string latestVersion, string currentVersion)
        {
            try
            {
                var latest = new Version(latestVersion.Trim());
                var current = new Version(currentVersion.Trim());
                return latest > current;
            }
            catch
            {
                // If version parsing fails, do a simple string comparison
                return !string.Equals(latestVersion.Trim(), currentVersion.Trim(), StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}