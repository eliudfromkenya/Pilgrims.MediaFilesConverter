using System;
using System.IO;
using System.Threading.Tasks;

namespace Pilgrims.MediaFilesConverter.Services.UtilityUpgrade
{
    /// <summary>
    /// Interface for archive extraction services
    /// </summary>
    public interface IExtractionService
    {
        /// <summary>
        /// Extracts an archive to the specified destination directory
        /// </summary>
        Task<bool> ExtractArchiveAsync(
            string archivePath, 
            string destinationDirectory, 
            IProgress<ExtractionProgress>? progress = null);

        /// <summary>
        /// Determines if the file is a supported archive format
        /// </summary>
        bool IsSupportedArchive(string filePath);

        /// <summary>
        /// Gets the extraction progress for a specific archive
        /// </summary>
        double GetExtractionProgress(string archivePath, string destinationDirectory);
    }

    /// <summary>
    /// Progress information for archive extraction
    /// </summary>
    public class ExtractionProgress
    {
        /// <summary>
        /// Total number of files in the archive
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Number of files extracted so far
        /// </summary>
        public int ExtractedFiles { get; set; }

        /// <summary>
        /// Current file being extracted
        /// </summary>
        public string? CurrentFile { get; set; }

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int Percentage => TotalFiles > 0 ? (int)((ExtractedFiles * 100) / TotalFiles) : 0;

        /// <summary>
        /// Total size of all files in bytes
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Bytes extracted so far
        /// </summary>
        public long ExtractedBytes { get; set; }

        /// <summary>
        /// Whether the extraction is complete
        /// </summary>
        public bool IsComplete { get; set; }
    }
}