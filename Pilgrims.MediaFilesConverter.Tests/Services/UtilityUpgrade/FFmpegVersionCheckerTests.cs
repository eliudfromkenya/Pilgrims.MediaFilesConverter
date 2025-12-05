using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Pilgrims.MediaFilesConverter.Tests.Services.UtilityUpgrade
{
    /// <summary>
    /// Unit tests for FFmpegVersionChecker
    /// </summary>
    public class FFmpegVersionCheckerTests
    {
        private readonly Mock<ILogger<FFmpegVersionChecker>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpHandler;
        private readonly HttpClient _httpClient;

        public FFmpegVersionCheckerTests()
        {
            _mockLogger = new Mock<ILogger<FFmpegVersionChecker>>();
            _mockHttpHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpHandler.Object);
        }

        [Fact]
        public async Task GetLatestVersionAsync_ShouldReturnVersion_WhenGitHubApiReturnsValidResponse()
        {
            // Arrange
            var expectedVersion = "6.1";
            var jsonResponse = @"{
                ""tag_name"": ""n6.1"",
                ""name"": ""n6.1"",
                ""published_at"": ""2024-01-01T00:00:00Z""
            }";

            _mockHttpHandler.SetupRequest(HttpMethod.Get, "https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/latest")
                .ReturnsJsonResponse(jsonResponse);

            var versionChecker = new FFmpegVersionChecker(_mockLogger.Object, _httpClient);

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

            _mockHttpHandler.SetupRequest(HttpMethod.Get, "https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/latest")
                .ReturnsJsonResponse(jsonResponse);

            var versionChecker = new FFmpegVersionChecker(_mockLogger.Object, _httpClient);

            // Act
            var result = await versionChecker.GetLatestVersionAsync();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetLatestVersionAsync_ShouldReturnEmptyString_WhenHttpRequestFails()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Get, "https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/latest")
                .ThrowsAsync(new HttpRequestException("Network error"));

            var versionChecker = new FFmpegVersionChecker(_mockLogger.Object, _httpClient);

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
            var output = "ffmpeg version 6.1 Copyright (c) 2000-2024 the FFmpeg developers";
            var expectedVersion = "6.1";

            var versionChecker = new FFmpegVersionChecker(_mockLogger.Object, _httpClient);

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

            var versionChecker = new FFmpegVersionChecker(_mockLogger.Object, _httpClient);

            // Act
            var result = await versionChecker.ExtractVersionFromCommandOutputAsync(output);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task CompareVersionsAsync_ShouldReturnUpdateAvailable_WhenCurrentIsOlder()
        {
            // Arrange
            var currentVersion = "6.0";
            var latestVersion = "6.1";

            var versionChecker = new FFmpegVersionChecker(_mockLogger.Object, _httpClient);

            // Act
            var result = await versionChecker.CompareVersionsAsync(currentVersion, latestVersion);

            // Assert
            Assert.Equal(VersionComparisonResult.UpdateAvailable, result);
        }

        [Fact]
        public async Task CompareVersionsAsync_ShouldReturnUpToDate_WhenVersionsAreEqual()
        {
            // Arrange
            var currentVersion = "6.1";
            var latestVersion = "6.1";

            var versionChecker = new FFmpegVersionChecker(_mockLogger.Object, _httpClient);

            // Act
            var result = await versionChecker.CompareVersionsAsync(currentVersion, latestVersion);

            // Assert
            Assert.Equal(VersionComparisonResult.UpToDate, result);
        }

        [Fact]
        public async Task CompareVersionsAsync_ShouldReturnNewerThanLatest_WhenCurrentIsNewer()
        {
            // Arrange
            var currentVersion = "6.2";
            var latestVersion = "6.1";

            var versionChecker = new FFmpegVersionChecker(_mockLogger.Object, _httpClient);

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
            var latestVersion = "6.1";

            var versionChecker = new FFmpegVersionChecker(_mockLogger.Object, _httpClient);

            // Act
            var result = await versionChecker.CompareVersionsAsync(currentVersion, latestVersion);

            // Assert
            Assert.Equal(VersionComparisonResult.ComparisonFailed, result);
        }
    }
}