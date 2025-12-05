using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Pilgrims.MediaFilesConverter.Services.UtilityUpgrade
{
    /// <summary>
    /// Archive extraction service implementation
    /// </summary>
    public class ExtractionService : IExtractionService
    {
        private readonly ILogger<ExtractionService> _logger;
        private readonly Dictionary<string, IArchiveExtractor> _extractors;

        public ExtractionService(ILogger<ExtractionService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize extractors for different archive formats
            _extractors = new Dictionary<string, IArchiveExtractor>(StringComparer.OrdinalIgnoreCase)
            {
                { ".zip", new ZipArchiveExtractor(logger) },
                { ".7z", new SevenZipArchiveExtractor(logger) },
                { ".tar.gz", new TarGzArchiveExtractor(logger) },
                { ".tar.bz2", new TarBz2ArchiveExtractor(logger) },
                { ".tar.xz", new TarXzArchiveExtractor(logger) }
            };
        }

        /// <summary>
        /// Extracts an archive to the specified destination directory
        /// </summary>
        public async Task<bool> ExtractArchiveAsync(
            string archivePath, 
            string destinationDirectory, 
            IProgress<ExtractionProgress>? progress = null)
        {
            try
            {
                _logger.LogInformation("Starting extraction of {ArchivePath} to {DestinationDirectory}", 
                    archivePath, destinationDirectory);

                if (!File.Exists(archivePath))
                {
                    _logger.LogError("Archive file does not exist: {ArchivePath}", archivePath);
                    return false;
                }

                // Ensure destination directory exists
                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                var extractor = GetExtractor(archivePath);
                if (extractor == null)
                {
                    _logger.LogError("No suitable extractor found for archive: {ArchivePath}", archivePath);
                    return false;
                }

                var result = await extractor.ExtractAsync(archivePath, destinationDirectory, progress);
                
                if (result)
                {
                    _logger.LogInformation("Successfully extracted {ArchivePath} to {DestinationDirectory}", 
                        archivePath, destinationDirectory);
                }
                else
                {
                    _logger.LogError("Failed to extract {ArchivePath}", archivePath);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting archive {ArchivePath} to {DestinationDirectory}", 
                    archivePath, destinationDirectory);
                return false;
            }
        }

        /// <summary>
        /// Determines if the file is a supported archive format
        /// </summary>
        public bool IsSupportedArchive(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
                // Handle compound extensions like .tar.gz
            if (extension == ".gz" || extension == ".bz2" || extension == ".xz")
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                if (Path.GetExtension(nameWithoutExt).ToLowerInvariant() == ".tar")
                {
                    extension = ".tar" + extension;
                }
            }

            return _extractors.ContainsKey(extension);
        }

        /// <summary>
        /// Gets the extraction progress for a specific archive
        /// </summary>
        public double GetExtractionProgress(string archivePath, string destinationDirectory)
        {
            try
            {
                var extractor = GetExtractor(archivePath);
                return extractor?.GetProgress(archivePath, destinationDirectory) ?? 0.0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting extraction progress for {ArchivePath}", archivePath);
                return 0.0;
            }
        }

        private IArchiveExtractor? GetExtractor(string archivePath)
        {
            var extension = Path.GetExtension(archivePath).ToLowerInvariant();
            
            // Handle compound extensions like .tar.gz
            if (extension == ".gz" || extension == ".bz2" || extension == ".xz")
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(archivePath);
                if (Path.GetExtension(nameWithoutExt).ToLowerInvariant() == ".tar")
                {
                    extension = ".tar" + extension;
                }
            }

            return _extractors.TryGetValue(extension, out var extractor) ? extractor : null;
        }
    }

    /// <summary>
    /// Interface for archive extractors
    /// </summary>
    public interface IArchiveExtractor
    {
        Task<bool> ExtractAsync(string archivePath, string destinationDirectory, IProgress<ExtractionProgress>? progress = null);
        double GetProgress(string archivePath, string destinationDirectory);
    }

    /// <summary>
    /// ZIP archive extractor
    /// </summary>
    public class ZipArchiveExtractor : IArchiveExtractor
    {
        private readonly ILogger _logger;

        public ZipArchiveExtractor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ExtractAsync(string archivePath, string destinationDirectory, IProgress<ExtractionProgress>? progress = null)
        {
            try
            {
                using var archive = ZipFile.OpenRead(archivePath);
                var entries = archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name)).ToList();
                
                var extractionProgress = new ExtractionProgress
                {
                    TotalFiles = entries.Count,
                    ExtractedFiles = 0,
                    TotalBytes = entries.Sum(e => e.Length),
                    ExtractedBytes = 0
                };

                foreach (var entry in entries)
                {
                    if (progress != null)
                    {
                        extractionProgress.CurrentFile = entry.FullName;
                        progress.Report(extractionProgress);
                    }

                    var destinationPath = Path.Combine(destinationDirectory, entry.FullName);
                    var destinationDir = Path.GetDirectoryName(destinationPath);
                    
                    if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }

                    entry.ExtractToFile(destinationPath, overwrite: true);
                    
                    extractionProgress.ExtractedFiles++;
                    extractionProgress.ExtractedBytes += entry.Length;
                    
                    if (progress != null)
                    {
                        progress.Report(extractionProgress);
                    }
                }

                extractionProgress.IsComplete = true;
                progress?.Report(extractionProgress);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting ZIP archive {ArchivePath}", archivePath);
                return false;
            }
        }

        public double GetProgress(string archivePath, string destinationDirectory)
        {
            try
            {
                if (!File.Exists(archivePath))
                    return 0.0;

                using var archive = ZipFile.OpenRead(archivePath);
                var totalFiles = archive.Entries.Count(e => !string.IsNullOrEmpty(e.Name));
                
                if (totalFiles == 0)
                    return 0.0;

                var extractedFiles = 0;
                foreach (var entry in archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name)))
                {
                    var destinationPath = Path.Combine(destinationDirectory, entry.FullName);
                    if (File.Exists(destinationPath))
                    {
                        extractedFiles++;
                    }
                }

                return (double)extractedFiles / totalFiles;
            }
            catch
            {
                return 0.0;
            }
        }
    }

    /// <summary>
    /// 7-Zip archive extractor (placeholder - would require 7z library)
    /// </summary>
    public class SevenZipArchiveExtractor : IArchiveExtractor
    {
        private readonly ILogger _logger;

        public SevenZipArchiveExtractor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<bool> ExtractAsync(string archivePath, string destinationDirectory, IProgress<ExtractionProgress>? progress = null)
        {
            _logger.LogWarning("7-Zip extraction not implemented. Please use ZIP format instead.");
            return Task.FromResult(false);
        }

        public double GetProgress(string archivePath, string destinationDirectory)
        {
            return 0.0;
        }
    }

    /// <summary>
    /// TAR.GZ archive extractor (placeholder - would require SharpZipLib or similar)
    /// </summary>
    public class TarGzArchiveExtractor : IArchiveExtractor
    {
        private readonly ILogger _logger;

        public TarGzArchiveExtractor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<bool> ExtractAsync(string archivePath, string destinationDirectory, IProgress<ExtractionProgress>? progress = null)
        {
            _logger.LogWarning("TAR.GZ extraction not implemented. Please use ZIP format instead.");
            return Task.FromResult(false);
        }

        public double GetProgress(string archivePath, string destinationDirectory)
        {
            return 0.0;
        }
    }

    /// <summary>
    /// TAR.BZ2 archive extractor (placeholder)
    /// </summary>
    public class TarBz2ArchiveExtractor : IArchiveExtractor
    {
        private readonly ILogger _logger;

        public TarBz2ArchiveExtractor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<bool> ExtractAsync(string archivePath, string destinationDirectory, IProgress<ExtractionProgress>? progress = null)
        {
            _logger.LogWarning("TAR.BZ2 extraction not implemented. Please use ZIP format instead.");
            return Task.FromResult(false);
        }

        public double GetProgress(string archivePath, string destinationDirectory)
        {
            return 0.0;
        }
    }

    /// <summary>
    /// TAR.XZ archive extractor (placeholder)
    /// </summary>
    public class TarXzArchiveExtractor : IArchiveExtractor
    {
        private readonly ILogger _logger;

        public TarXzArchiveExtractor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<bool> ExtractAsync(string archivePath, string destinationDirectory, IProgress<ExtractionProgress>? progress = null)
        {
            _logger.LogWarning("TAR.XZ extraction not implemented. Please use ZIP format instead.");
            return Task.FromResult(false);
        }

        public double GetProgress(string archivePath, string destinationDirectory)
        {
            return 0.0;
        }
    }
}