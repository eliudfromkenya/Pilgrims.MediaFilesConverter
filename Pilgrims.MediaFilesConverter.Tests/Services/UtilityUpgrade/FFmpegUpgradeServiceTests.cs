using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Pilgrims.MediaFilesConverter.Models;

namespace Pilgrims.MediaFilesConverter.Tests.Services.UtilityUpgrade
{
    /// <summary>
    /// Unit tests for FFmpegUpgradeService
    /// </summary>
    public class FFmpegUpgradeServiceTests
    {
        private readonly Mock<ILogger<FFmpegUpgradeService>> _mockLogger;
        private readonly Mock<IVersionChecker> _mockVersionChecker;
        private readonly Mock<IDownloadService> _mockDownloadService;
        private readonly Mock<IExtractionService> _mockExtractionService;
        private readonly Mock<IUtilityConfigurationService> _mockConfigurationService;
        private readonly FFmpegUpgradeService _ffmpegUpgradeService;

        public FFmpegUpgradeServiceTests()
        {
            _mockLogger = new Mock<ILogger<FFmpegUpgradeService>>();
            _mockVersionChecker = new Mock<IVersionChecker>();
            _mockDownloadService = new Mock<IDownloadService>();
            _mockExtractionService = new Mock<IExtractionService>();
            _mockConfigurationService = new Mock<IUtilityConfigurationService>();

            _ffmpegUpgradeService = new FFmpegUpgradeService(
                _mockLogger.Object,
                _mockVersionChecker.Object,
                _mockDownloadService.Object,
                _mockExtractionService.Object,
                _mockConfigurationService.Object);
        }

        [Fact]
        public async Task GetUtilityInfoAsync_ShouldReturnInfo_WhenUtilityIsAvailable()
        {
            // Arrange
            var currentVersion = "4.4.0";
            var latestVersion = "5.0.0";
            var executablePath = "/usr/bin/ffmpeg";

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockVersionChecker.Setup(x => x.CompareVersionsAsync(currentVersion, latestVersion))
                .ReturnsAsync(VersionComparisonResult.UpdateAvailable);
            _mockConfigurationService.Setup(x => x.GetUtilityPathAsync("ffmpeg")).ReturnsAsync(executablePath);
            _mockConfigurationService.Setup(x => x.ValidateUtilityPathAsync(executablePath)).ReturnsAsync(true);

            // Act
            var result = await _ffmpegUpgradeService.GetUtilityInfoAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ffmpeg", result.Name);
            Assert.Equal(currentVersion, result.CurrentVersion);
            Assert.Equal(latestVersion, result.LatestVersion);
            Assert.Equal(executablePath, result.ExecutablePath);
            Assert.True(result.IsAvailable);
            Assert.True(result.IsUpdateAvailable);
            Assert.Equal(UpdateStatus.UpdateAvailable, result.UpdateStatus);
        }

        [Fact]
        public async Task GetUtilityInfoAsync_ShouldReturnInfo_WhenUtilityIsNotAvailable()
        {
            // Arrange
            var currentVersion = string.Empty;
            var latestVersion = "5.0.0";
            var executablePath = string.Empty;

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockConfigurationService.Setup(x => x.GetUtilityPathAsync("ffmpeg")).ReturnsAsync(executablePath);

            // Act
            var result = await _ffmpegUpgradeService.GetUtilityInfoAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ffmpeg", result.Name);
            Assert.Equal(currentVersion, result.CurrentVersion);
            Assert.Equal(latestVersion, result.LatestVersion);
            Assert.Equal(executablePath, result.ExecutablePath);
            Assert.False(result.IsAvailable);
            Assert.False(result.IsUpdateAvailable);
            Assert.Equal(UpdateStatus.NotInstalled, result.UpdateStatus);
        }

        [Fact]
        public async Task CheckForUpdatesAsync_ShouldReturnUpdateAvailable_WhenUpdateIsAvailable()
        {
            // Arrange
            var currentVersion = "4.4.0";
            var latestVersion = "5.0.0";

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockVersionChecker.Setup(x => x.CompareVersionsAsync(currentVersion, latestVersion))
                .ReturnsAsync(VersionComparisonResult.UpdateAvailable);

            // Act
            var result = await _ffmpegUpgradeService.CheckForUpdatesAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CheckForUpdatesAsync_ShouldReturnFalse_WhenNoUpdateIsAvailable()
        {
            // Arrange
            var currentVersion = "5.0.0";
            var latestVersion = "5.0.0";

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockVersionChecker.Setup(x => x.CompareVersionsAsync(currentVersion, latestVersion))
                .ReturnsAsync(VersionComparisonResult.UpToDate);

            // Act
            var result = await _ffmpegUpgradeService.CheckForUpdatesAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpgradeAsync_ShouldReturnSuccess_WhenUpgradeSucceeds()
        {
            // Arrange
            var currentVersion = "4.4.0";
            var latestVersion = "5.0.0";
            var downloadUrl = "https://example.com/ffmpeg.zip";
            var downloadPath = Path.Combine(Path.GetTempPath(), "ffmpeg.zip");
            var extractPath = Path.Combine(Path.GetTempPath(), "ffmpeg_extract");
            var finalPath = "/usr/bin/ffmpeg";

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockConfigurationService.Setup(x => x.GetDefaultTempDirectory()).Returns(Path.GetTempPath());
            _mockConfigurationService.Setup(x => x.ResolveUtilityPath("ffmpeg")).Returns(finalPath);
            _mockDownloadService.Setup(x => x.GetFileSizeAsync(downloadUrl)).ReturnsAsync(10 * 1024 * 1024);
            _mockDownloadService.Setup(x => x.DownloadFileAsync(downloadUrl, It.IsAny<string>(), It.IsAny<IProgress<DownloadProgress>>()))
                .ReturnsAsync(true);
            _mockExtractionService.Setup(x => x.ExtractArchiveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IProgress<ExtractionProgress>>()))
                .ReturnsAsync(true);

            var progressReporter = new Progress<UpgradeProgress>();

            // Act
            var result = await _ffmpegUpgradeService.UpgradeAsync(progressReporter);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("FFmpeg upgraded successfully from 4.4.0 to 5.0.0", result.Message);
            _mockConfigurationService.Verify(x => x.SetUtilityPathAsync("ffmpeg", finalPath), Times.Once);
        }

        [Fact]
        public async Task UpgradeAsync_ShouldReturnFailure_WhenDownloadFails()
        {
            // Arrange
            var currentVersion = "4.4.0";
            var latestVersion = "5.0.0";
            var downloadUrl = "https://example.com/ffmpeg.zip";

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockConfigurationService.Setup(x => x.GetDefaultTempDirectory()).Returns(Path.GetTempPath());
            _mockDownloadService.Setup(x => x.GetFileSizeAsync(downloadUrl)).ReturnsAsync(10 * 1024 * 1024);
            _mockDownloadService.Setup(x => x.DownloadFileAsync(downloadUrl, It.IsAny<string>(), It.IsAny<IProgress<DownloadProgress>>()))
                .ReturnsAsync(false);

            var progressReporter = new Progress<UpgradeProgress>();

            // Act
            var result = await _ffmpegUpgradeService.UpgradeAsync(progressReporter);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Failed to download FFmpeg", result.Message);
        }

        [Fact]
        public async Task UpgradeAsync_ShouldReturnFailure_WhenExtractionFails()
        {
            // Arrange
            var currentVersion = "4.4.0";
            var latestVersion = "5.0.0";
            var downloadUrl = "https://example.com/ffmpeg.zip";

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockConfigurationService.Setup(x => x.GetDefaultTempDirectory()).Returns(Path.GetTempPath());
            _mockDownloadService.Setup(x => x.GetFileSizeAsync(downloadUrl)).ReturnsAsync(10 * 1024 * 1024);
            _mockDownloadService.Setup(x => x.DownloadFileAsync(downloadUrl, It.IsAny<string>(), It.IsAny<IProgress<DownloadProgress>>()))
                .ReturnsAsync(true);
            _mockExtractionService.Setup(x => x.ExtractArchiveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IProgress<ExtractionProgress>>()))
                .ReturnsAsync(false);

            var progressReporter = new Progress<UpgradeProgress>();

            // Act
            var result = await _ffmpegUpgradeService.UpgradeAsync(progressReporter);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Failed to extract FFmpeg", result.Message);
        }

        [Fact]
        public async Task UpgradeAsync_ShouldReturnFailure_WhenVersionCheckFails()
        {
            // Arrange
            var currentVersion = string.Empty;
            var latestVersion = "5.0.0";

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockVersionChecker.Setup(x => x.CompareVersionsAsync(currentVersion, latestVersion))
                .ReturnsAsync(VersionComparisonResult.ComparisonFailed);

            var progressReporter = new Progress<UpgradeProgress>();

            // Act
            var result = await _ffmpegUpgradeService.UpgradeAsync(progressReporter);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Failed to check for updates", result.Message);
        }

        [Fact]
        public void GetUtilityName_ShouldReturnFFmpeg()
        {
            // Act
            var result = _ffmpegUpgradeService.GetUtilityName();

            // Assert
            Assert.Equal("ffmpeg", result);
        }

        [Fact]
        public async Task UpgradeAsync_ShouldHandleCancellation_WhenCancelled()
        {
            // Arrange
            var currentVersion = "4.4.0";
            var latestVersion = "5.0.0";
            var downloadUrl = "https://example.com/ffmpeg.zip";
            var cancellationToken = new CancellationTokenSource();
            var progressReporter = new Progress<UpgradeProgress>();

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockConfigurationService.Setup(x => x.GetDefaultTempDirectory()).Returns(Path.GetTempPath());
            _mockDownloadService.Setup(x => x.GetFileSizeAsync(downloadUrl)).ReturnsAsync(10 * 1024 * 1024);
            _mockDownloadService.Setup(x => x.DownloadFileAsync(downloadUrl, It.IsAny<string>(), It.IsAny<IProgress<DownloadProgress>>(), cancellationToken.Token))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _ffmpegUpgradeService.UpgradeAsync(progressReporter, cancellationToken.Token));
        }

        [Fact]
        public async Task UpgradeAsync_ShouldReturnUpToDate_WhenAlreadyUpToDate()
        {
            // Arrange
            var currentVersion = "5.0.0";
            var latestVersion = "5.0.0";

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockVersionChecker.Setup(x => x.CompareVersionsAsync(currentVersion, latestVersion))
                .ReturnsAsync(VersionComparisonResult.UpToDate);

            var progressReporter = new Progress<UpgradeProgress>();

            // Act
            var result = await _ffmpegUpgradeService.UpgradeAsync(progressReporter);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Contains("already up to date", result.Message);
        }
    }
}