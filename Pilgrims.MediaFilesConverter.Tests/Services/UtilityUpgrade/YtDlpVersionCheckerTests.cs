using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Pilgrims.MediaFilesConverter.Tests.Services.UtilityUpgrade
{
    /// <summary>
    /// Unit tests for YtDlpVersionChecker
    /// </summary>
    public class YtDlpVersionCheckerTests
    {
        private readonly Mock<ILogger<YtDlpVersionChecker>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpHandler;
        private readonly HttpClient _httpClient;

        public YtDlpVersionCheckerTests()
        {
            _mockLogger = new Mock<ILogger<YtDlpVersionChecker>>();
            _mockHttpHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpHandler.Object);
        }

        [Fact]
        public async Task GetLatestVersionAsync_ShouldReturnVersion_WhenGitHubApiReturnsValidResponse()
        {
            // Arrange
            var expectedVersion = "2024.01.01";
            var jsonResponse = @"{
                ""tag_name"": ""2024.01.01"",
                ""name"": ""yt-dlp 2024.01.01"",
                ""published_at"": ""2024-01-01T00:00:00Z""
            }";

            _mockHttpHandler.SetupRequest(HttpMethod.Get, "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest")
                .ReturnsJsonResponse(jsonResponse);

            var versionChecker = new YtDlpVersionChecker(_mockLogger.Object, _httpClient);

            // Act
            var result = await versionChecker.GetLatestVersionAsync();

            // Assert
            Assert.Equal(expectedVersion, result);
        }

        [Fact]
        public async Task GetLatestVersionAsync_ShouldReturnEmptyString_WhenGitHubApiReturnsInvalidResponse()
        {
            // Arrange
            var jsonResponse = @"{""invalid"": ""response""}";

            _mockHttpHandler.SetupRequest(HttpMethod.Get, "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest")
                .ReturnsJsonResponse(jsonResponse);

            var versionChecker = new YtDlpVersionChecker(_mockLogger.Object, _httpClient);

            // Act
            var result = await versionChecker.GetLatestVersionAsync();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetLatestVersionAsync_ShouldReturnEmptyString_WhenHttpRequestFails()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Get, "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest")
                .ThrowsAsync(new HttpRequestException("Network error"));

            var versionChecker = new YtDlpVersionChecker(_mockLogger.Object, _httpClient);

            // Act
            var result = await versionChecker.GetLatestVersionAsync();

            // Assert
            Assert.Equal(string.Empty, result);
            _mockLogger.VerifyLogErrorContains("Network error");
        }

        [Fact]
        public async Task ExtractVersionFromCommandOutput_ShouldReturnVersion_WhenOutputContainsVersion()
        {
            // Arrange
            var output = "yt-dlp version 2024.01.01";
            var expectedVersion = "2024.01.01";

            var versionChecker = new YtDlpVersionChecker(_mockLogger.Object, _httpClient);

            // Act
            var result = await versionChecker.ExtractVersionFromCommandOutputAsync(output);

            // Assert
            Assert.Equal(expectedVersion, result);
        }

        [Fact]
        public async Task ExtractVersionFromCommandOutput_ShouldReturnEmptyString_WhenOutputDoesNotContainVersion()
        {
            // Arrange
            var output = "Invalid output";

            var versionChecker = new YtDlpVersionChecker(_mockLogger.Object, _httpClient);

            // Act
            var result = await versionChecker.ExtractVersionFromCommandOutputAsync(output);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task CompareVersionsAsync_ShouldReturnUpdateAvailable_WhenCurrentIsOlder()
        {
            // Arrange
            var currentVersion = "2023.12.31";
            var latestVersion = "2024.01.01";

            var versionChecker = new YtDlpVersionChecker(_mockLogger.Object, _httpClient);

            // Act
            var result = await versionChecker.CompareVersionsAsync(currentVersion, latestVersion);

            // Assert
            Assert.Equal(VersionComparisonResult.UpdateAvailable, result);
        }

        [Fact]
        public async Task CompareVersionsAsync_ShouldReturnUpToDate_WhenVersionsAreEqual()
        {
            // Arrange
            var currentVersion = "2024.01.01";
            var latestVersion = "2024.01.01";

            var versionChecker = new YtDlpVersionChecker(_mockLogger.Object, _httpClient);

            // Act
            var result = await versionChecker.CompareVersionsAsync(currentVersion, latestVersion);

            // Assert
            Assert.Equal(VersionComparisonResult.UpToDate, result);
        }

        [Fact]
        public async Task CompareVersionsAsync_ShouldReturnNewerThanLatest_WhenCurrentIsNewer()
        {
            // Arrange
            var currentVersion = "2024.01.02";
            var latestVersion = "2024.01.01";

            var versionChecker = new YtDlpVersionChecker(_mockLogger.Object, _httpClient);

            // Act
            var result = await versionChecker.CompareVersionsAsync(currentVersion, latestVersion);

            // Assert
            Assert.Equal(VersionComparisonResult.NewerThanLatest, result);
        }

        [Fact]
        public async Task CompareVersionsAsync_ShouldReturnComparisonFailed_WhenVersionsAreInvalid()
        {
            // Arrange
            var currentVersion = "invalid";
            var latestVersion = "2024.01.01";

            var versionChecker = new YtDlpVersionChecker(_mockLogger.Object, _httpClient);

            // Act
            var result = await versionChecker.CompareVersionsAsync(currentVersion, latestVersion);

            // Assert
            Assert.Equal(VersionComparisonResult.ComparisonFailed, result);
        }
    }
}