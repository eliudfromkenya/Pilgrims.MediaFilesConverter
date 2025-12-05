using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Pilgrims.MediaFilesConverter.Services.UtilityUpgrade
{
    /// <summary>
    /// Interface for secure file download services
    /// </summary>
    public interface IDownloadService
    {
        /// <summary>
        /// Downloads a file from the specified URL
        /// </summary>
        Task<bool> DownloadFileAsync(
            string url, 
            string destinationPath, 
            IProgress<DownloadProgress>? progress = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the size of the file at the specified URL
        /// </summary>
        Task<long?> GetFileSizeAsync(string url, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates the downloaded file (checksum, signature, etc.)
        /// </summary>
        Task<bool> ValidateDownloadedFileAsync(string filePath, string? expectedChecksum = null);
    }

    /// <summary>
    /// Progress information for file downloads
    /// </summary>
    public class DownloadProgress
    {
        /// <summary>
        /// Total bytes to download
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Bytes downloaded so far
        /// </summary>
        public long BytesDownloaded { get; set; }

        /// <summary>
        /// Download speed in bytes per second
        /// </summary>
        public long BytesPerSecond { get; set; }

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int Percentage => TotalBytes > 0 ? (int)((BytesDownloaded * 100) / TotalBytes) : 0;

        /// <summary>
        /// Estimated time remaining in seconds
        /// </summary>
        public int? EstimatedTimeRemaining
        {
            get
            {
                if (BytesPerSecond <= 0 || TotalBytes <= BytesDownloaded)
                    return null;

                var remainingBytes = TotalBytes - BytesDownloaded;
                return (int)(remainingBytes / BytesPerSecond);
            }
        }
    }
}