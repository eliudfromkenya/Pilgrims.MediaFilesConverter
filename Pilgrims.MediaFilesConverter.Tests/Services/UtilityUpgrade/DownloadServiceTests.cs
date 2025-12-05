using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Pilgrims.MediaFilesConverter.Tests.Services.UtilityUpgrade
{
    /// <summary>
    /// Unit tests for DownloadService
    /// </summary>
    public class DownloadServiceTests
    {
        private readonly Mock<ILogger<DownloadService>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpHandler;
        private readonly HttpClient _httpClient;

        public DownloadServiceTests()
        {
            _mockLogger = new Mock<ILogger<DownloadService>>();
            _mockHttpHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpHandler.Object);
        }

        [Fact]
        public async Task DownloadFileAsync_ShouldDownloadFile_WhenValidUrlProvided()
        {
            // Arrange
            var downloadUrl = "https://example.com/file.zip";
            var destinationPath = Path.GetTempFileName();
            var fileContent = new byte[] { 1, 2, 3, 4, 5 };
            var progressReporter = new Progress<DownloadProgress>();

            _mockHttpHandler.SetupRequest(HttpMethod.Get, downloadUrl)
                .ReturnsResponse(System.Net.HttpStatusCode.OK, fileContent);

            var downloadService = new DownloadService(_mockLogger.Object, _httpClient);

            // Act
            var result = await downloadService.DownloadFileAsync(downloadUrl, destinationPath, progressReporter);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(destinationPath));
            var downloadedBytes = await File.ReadAllBytesAsync(destinationPath);
            Assert.Equal(fileContent, downloadedBytes);

            // Cleanup
            File.Delete(destinationPath);
        }

        [Fact]
        public async Task DownloadFileAsync_ShouldReportProgress_WhenDownloading()
        {
            // Arrange
            var downloadUrl = "https://example.com/file.zip";
            var destinationPath = Path.GetTempFileName();
            var fileContent = new byte[1024 * 10]; // 10KB file
            var progressUpdates = new List<DownloadProgress>();
            var progressReporter = new Progress<DownloadProgress>(progress => progressUpdates.Add(progress));

            _mockHttpHandler.SetupRequest(HttpMethod.Get, downloadUrl)
                .ReturnsResponse(System.Net.HttpStatusCode.OK, fileContent);

            var downloadService = new DownloadService(_mockLogger.Object, _httpClient);

            // Act
            var result = await downloadService.DownloadFileAsync(downloadUrl, destinationPath, progressReporter);

            // Assert
            Assert.True(result);
            Assert.NotEmpty(progressUpdates);
            Assert.True(progressUpdates.Any(p => p.TotalBytes > 0));
            Assert.True(progressUpdates.Any(p => p.BytesDownloaded > 0));

            // Cleanup
            File.Delete(destinationPath);
        }

        [Fact]
        public async Task DownloadFileAsync_ShouldReturnFalse_WhenHttpRequestFails()
        {
            // Arrange
            var downloadUrl = "https://example.com/file.zip";
            var destinationPath = Path.GetTempFileName();
            var progressReporter = new Progress<DownloadProgress>();

            _mockHttpHandler.SetupRequest(HttpMethod.Get, downloadUrl)
                .ThrowsAsync(new HttpRequestException("Network error"));

            var downloadService = new DownloadService(_mockLogger.Object, _httpClient);

            // Act
            var result = await downloadService.DownloadFileAsync(downloadUrl, destinationPath, progressReporter);

            // Assert
            Assert.False(result);
            Assert.False(File.Exists(destinationPath));
            _mockLogger.VerifyLogErrorContains("Network error");
        }

        [Fact]
        public async Task DownloadFileAsync_ShouldHandleCancellation_WhenCancelled()
        {
            // Arrange
            var downloadUrl = "https://example.com/file.zip";
            var destinationPath = Path.GetTempFileName();
            var progressReporter = new Progress<DownloadProgress>();
            var cancellationToken = new CancellationTokenSource();
            var fileContent = new byte[1024 * 100]; // 100KB file to simulate longer download

            _mockHttpHandler.SetupRequest(HttpMethod.Get, downloadUrl)
                .ReturnsResponse(System.Net.HttpStatusCode.OK, fileContent)
                .Callback(() => cancellationToken.Cancel());

            var downloadService = new DownloadService(_mockLogger.Object, _httpClient);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => downloadService.DownloadFileAsync(downloadUrl, destinationPath, progressReporter, cancellationToken.Token));

            // Cleanup
            File.Delete(destinationPath);
        }

        [Fact]
        public async Task GetFileSizeAsync_ShouldReturnSize_WhenUrlIsValid()
        {
            // Arrange
            var downloadUrl = "https://example.com/file.zip";
            var expectedSize = 1024L;

            _mockHttpHandler.SetupRequest(HttpMethod.Head, downloadUrl)
                .ReturnsResponse(System.Net.HttpStatusCode.OK, new byte[0], new[] {
                    new System.Net.Http.Headers.KeyValuePair<string, IEnumerable<string>>("Content-Length", new[] { expectedSize.ToString() })
                });

            var downloadService = new DownloadService(_mockLogger.Object, _httpClient);

            // Act
            var result = await downloadService.GetFileSizeAsync(downloadUrl);

            // Assert
            Assert.Equal(expectedSize, result);
        }

        [Fact]
        public async Task GetFileSizeAsync_ShouldReturnZero_WhenUrlIsInvalid()
        {
            // Arrange
            var downloadUrl = "https://example.com/invalid-file.zip";

            _mockHttpHandler.SetupRequest(HttpMethod.Head, downloadUrl)
                .ReturnsResponse(System.Net.HttpStatusCode.NotFound);

            var downloadService = new DownloadService(_mockLogger.Object, _httpClient);

            // Act
            var result = await downloadService.GetFileSizeAsync(downloadUrl);

            // Assert
            Assert.Equal(0L, result);
        }

        [Fact]
        public async Task ValidateFileAsync_ShouldReturnTrue_WhenChecksumMatches()
        {
            // Arrange
            var filePath = Path.GetTempFileName();
            var fileContent = new byte[] { 1, 2, 3, 4, 5 };
            await File.WriteAllBytesAsync(filePath, fileContent);
            var expectedChecksum = "8b1a9953c4611296a827abf8c47804d7"; // MD5 of the file content

            var downloadService = new DownloadService(_mockLogger.Object, _httpClient);

            // Act
            var result = await downloadService.ValidateFileAsync(filePath, expectedChecksum);

            // Assert
            Assert.True(result);

            // Cleanup
            File.Delete(filePath);
        }

        [Fact]
        public async Task ValidateFileAsync_ShouldReturnFalse_WhenChecksumDoesNotMatch()
        {
            // Arrange
            var filePath = Path.GetTempFileName();
            var fileContent = new byte[] { 1, 2, 3, 4, 5 };
            await File.WriteAllBytesAsync(filePath, fileContent);
            var expectedChecksum = "invalid-checksum";

            var downloadService = new DownloadService(_mockLogger.Object, _httpClient);

            // Act
            var result = await downloadService.ValidateFileAsync(filePath, expectedChecksum);

            // Assert
            Assert.False(result);

            // Cleanup
            File.Delete(filePath);
        }

        [Fact]
        public async Task ValidateFileAsync_ShouldReturnFalse_WhenFileDoesNotExist()
        {
            // Arrange
            var filePath = "non-existent-file.txt";
            var expectedChecksum = "any-checksum";

            var downloadService = new DownloadService(_mockLogger.Object, _httpClient);

            // Act
            var result = await downloadService.ValidateFileAsync(filePath, expectedChecksum);

            // Assert
            Assert.False(result);
            _mockLogger.VerifyLogErrorContains("File not found");
        }
    }
}