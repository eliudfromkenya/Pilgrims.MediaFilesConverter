using System;

namespace Pilgrims.MediaFilesConverter.Models
{
    /// <summary>
    /// Represents the progress of a utility upgrade operation
    /// </summary>
    public class UpgradeProgress
    {
        /// <summary>
        /// Name of the utility being upgraded
        /// </summary>
        public string UtilityName { get; set; } = string.Empty;

        /// <summary>
        /// Current status of the upgrade operation
        /// </summary>
        public UpgradeStatus Status { get; set; }

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int Percentage { get; set; }

        /// <summary>
        /// Current operation being performed
        /// </summary>
        public string CurrentOperation { get; set; } = string.Empty;

        /// <summary>
        /// Additional details about the current operation
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Whether the operation has completed (successfully or with errors)
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Timestamp when the operation started
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Timestamp when the operation completed
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Estimated time remaining (in seconds)
        /// </summary>
        public int? EstimatedTimeRemaining { get; set; }
    }

    /// <summary>
    /// Represents the status of an upgrade operation
    /// </summary>
    public enum UpgradeStatus
    {
        /// <summary>
        /// Operation has not started
        /// </summary>
        NotStarted,

        /// <summary>
        /// Operation has started
        /// </summary>
        Started,

        /// <summary>
        /// Checking for available updates
        /// </summary>
        CheckingForUpdates,

        /// <summary>
        /// Downloading the utility
        /// </summary>
        Downloading,

        /// <summary>
        /// Extracting the downloaded file
        /// </summary>
        Extracting,

        /// <summary>
        /// Verifying the installation
        /// </summary>
        Verifying,

        /// <summary>
        /// Operation completed successfully
        /// </summary>
        Completed,

        /// <summary>
        /// Operation failed with errors
        /// </summary>
        Failed,

        /// <summary>
        /// Operation was cancelled
        /// </summary>
        Cancelled
    }
}