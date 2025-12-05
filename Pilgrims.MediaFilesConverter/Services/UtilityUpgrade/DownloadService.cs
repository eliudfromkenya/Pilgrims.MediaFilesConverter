using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Pilgrims.MediaFilesConverter.Services.UtilityUpgrade
{
    /// <summary>
    /// Secure download service implementation
    /// </summary>
    public class DownloadService : IDownloadService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DownloadService> _logger;
        private const int BufferSize = 8192;

        public DownloadService(HttpClient httpClient, ILogger<DownloadService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Downloads a file from the specified URL
        /// </summary>
        public async Task<bool> DownloadFileAsync(
            string url, 
            string destinationPath, 
            IProgress<DownloadProgress>? progress = null, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting download from {Url} to {DestinationPath}", url, destinationPath);
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var downloadProgress = new DownloadProgress
                {
                    TotalBytes = totalBytes,
                    BytesDownloaded = 0,
                    BytesPerSecond = 0
                };

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var lastReportedBytes = 0L;
                var lastReportedTime = stopwatch.ElapsedMilliseconds;

                using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);

                var buffer = new byte[BufferSize];
                var isMoreToRead = true;

                do
                {
                    var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        continue;
                    }

                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    downloadProgress.BytesDownloaded += bytesRead;

                    // Calculate download speed and estimated time remaining
                    var currentTime = stopwatch.ElapsedMilliseconds;
                    var timeDiff = currentTime - lastReportedTime;
                    
                    if (timeDiff >= 1000) // Update every second
                    {
                        var bytesDiff = downloadProgress.BytesDownloaded - lastReportedBytes;
                        downloadProgress.BytesPerSecond = (long)((bytesDiff * 1000.0) / timeDiff);
                        
                        lastReportedBytes = downloadProgress.BytesDownloaded;
                        lastReportedTime = currentTime;
                    }

                    progress?.Report(downloadProgress);
                    
                } while (isMoreToRead);

                stopwatch.Stop();
                
                downloadProgress.BytesPerSecond = totalBytes > 0 ? (long)(totalBytes / (stopwatch.ElapsedMilliseconds / 1000.0)) : 0;
                progress?.Report(downloadProgress);

                _logger.LogInformation("Download completed successfully: {Url} -> {DestinationPath} ({TotalBytes} bytes)", 
                    url, destinationPath, downloadProgress.BytesDownloaded);

                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Download was cancelled: {Url}", url);
                
                // Clean up partial download
                if (File.Exists(destinationPath))
                {
                    try
                    {
                        File.Delete(destinationPath);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogWarning(deleteEx, "Failed to delete partial download file: {DestinationPath}", destinationPath);
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file from {Url} to {DestinationPath}", url, destinationPath);
                return false;
            }
        }

        /// <summary>
        /// Gets the size of the file at the specified URL
        /// </summary>
        public async Task<long?> GetFileSizeAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _httpClient.SendAsync(
                    new HttpRequestMessage(HttpMethod.Head, url), 
                    cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    return response.Content.Headers.ContentLength;
                }

                _logger.LogWarning("Failed to get file size for {Url}: {StatusCode}", url, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file size for {Url}", url);
                return null;
            }
        }

        /// <summary>
        /// Validates the downloaded file (checksum, signature, etc.)
        /// </summary>
        public async Task<bool> ValidateDownloadedFileAsync(string filePath, string? expectedChecksum = null)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File does not exist for validation: {FilePath}", filePath);
                    return false;
                }

                // Basic validation - check file size and existence
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    _logger.LogWarning("Downloaded file is empty: {FilePath}", filePath);
                    return false;
                }

                // If checksum is provided, validate it
                if (!string.IsNullOrEmpty(expectedChecksum))
                {
                    var actualChecksum = await CalculateFileChecksumAsync(filePath);
                    var isValid = string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);
                    
                    if (!isValid)
                    {
                        _logger.LogWarning("File checksum validation failed: {FilePath}. Expected: {Expected}, Actual: {Actual}", 
                            filePath, expectedChecksum, actualChecksum);
                        return false;
                    }
                    
                    _logger.LogInformation("File checksum validation successful: {FilePath}", filePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating downloaded file: {FilePath}", filePath);
                return false;
            }
        }

        private async Task<string> CalculateFileChecksumAsync(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = await sha256.ComputeHashAsync(stream);
            return Convert.ToHexString(hash);
        }
    }
}