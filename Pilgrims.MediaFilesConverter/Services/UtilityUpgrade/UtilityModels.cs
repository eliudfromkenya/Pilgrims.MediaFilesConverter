using System;

namespace Pilgrims.MediaFilesConverter.Services.UtilityUpgrade
{
    /// <summary>
    /// Represents the status of an upgrade operation
    /// </summary>
    public enum UpgradeStatus
    {
        /// <summary>
        /// Operation has started
        /// </summary>
        Started,

        /// <summary>
        /// Operation is in progress
        /// </summary>
        InProgress,

        /// <summary>
        /// Operation completed successfully
        /// </summary>
        Completed,

        /// <summary>
        /// Operation failed
        /// </summary>
        Failed,

        /// <summary>
        /// Operation was cancelled
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// Represents the result of an upgrade operation
    /// </summary>
    public class UpgradeResult
    {
        /// <summary>
        /// Whether the upgrade was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Success or informational message
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Error message if the upgrade failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// New version that was installed
        /// </summary>
        public string? NewVersion { get; set; }

        /// <summary>
        /// Previous version before upgrade
        /// </summary>
        public string? PreviousVersion { get; set; }
    }

    /// <summary>
    /// Represents information about available updates
    /// </summary>
    public class UpdateCheckResult
    {
        /// <summary>
        /// Whether an update is available
        /// </summary>
        public bool UpdateAvailable { get; set; }

        /// <summary>
        /// Current installed version
        /// </summary>
        public string? CurrentVersion { get; set; }

        /// <summary>
        /// Latest available version
        /// </summary>
        public string? LatestVersion { get; set; }

        /// <summary>
        /// Error message if check failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}

