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
    /// Unit tests for YtDlpUpgradeService
    /// </summary>
    public class YtDlpUpgradeServiceTests
    {
        private readonly Mock<ILogger<YtDlpUpgradeService>> _mockLogger;
        private readonly Mock<IVersionChecker> _mockVersionChecker;
        private readonly Mock<IDownloadService> _mockDownloadService;
        private readonly Mock<IExtractionService> _mockExtractionService;
        private readonly Mock<IUtilityConfigurationService> _mockConfigurationService;
        private readonly YtDlpUpgradeService _ytDlpUpgradeService;

        public YtDlpUpgradeServiceTests()
        {
            _mockLogger = new Mock<ILogger<YtDlpUpgradeService>>();
            _mockVersionChecker = new Mock<IVersionChecker>();
            _mockDownloadService = new Mock<IDownloadService>();
            _mockExtractionService = new Mock<IExtractionService>();
            _mockConfigurationService = new Mock<IUtilityConfigurationService>();

            _ytDlpUpgradeService = new YtDlpUpgradeService(
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
            var currentVersion = "2023.01.01";
            var latestVersion = "2023.02.01";
            var executablePath = "/usr/bin/yt-dlp";

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockVersionChecker.Setup(x => x.CompareVersionsAsync(currentVersion, latestVersion))
                .ReturnsAsync(VersionComparisonResult.UpdateAvailable);
            _mockConfigurationService.Setup(x => x.GetUtilityPathAsync("yt-dlp")).ReturnsAsync(executablePath);
            _mockConfigurationService.Setup(x => x.ValidateUtilityPathAsync(executablePath)).ReturnsAsync(true);

            // Act
            var result = await _ytDlpUpgradeService.GetUtilityInfoAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("yt-dlp", result.Name);
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
            var latestVersion = "2023.02.01";
            var executablePath = string.Empty;

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockConfigurationService.Setup(x => x.GetUtilityPathAsync("yt-dlp")).ReturnsAsync(executablePath);

            // Act
            var result = await _ytDlpUpgradeService.GetUtilityInfoAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("yt-dlp", result.Name);
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
            var currentVersion = "2023.01.01";
            var latestVersion = "2023.02.01";

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockVersionChecker.Setup(x => x.CompareVersionsAsync(currentVersion, latestVersion))
                .ReturnsAsync(VersionComparisonResult.UpdateAvailable);

            // Act
            var result = await _ytDlpUpgradeService.CheckForUpdatesAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CheckForUpdatesAsync_ShouldReturnFalse_WhenNoUpdateIsAvailable()
        {
            // Arrange
            var currentVersion = "2023.02.01";
            var latestVersion = "2023.02.01";

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockVersionChecker.Setup(x => x.CompareVersionsAsync(currentVersion, latestVersion))
                .ReturnsAsync(VersionComparisonResult.UpToDate);

            // Act
            var result = await _ytDlpUpgradeService.CheckForUpdatesAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpgradeAsync_ShouldReturnSuccess_WhenUpgradeSucceeds()
        {
            // Arrange
            var currentVersion = "2023.01.01";
            var latestVersion = "2023.02.01";
            var downloadUrl = "https://example.com/yt-dlp.exe";
            var downloadPath = Path.Combine(Path.GetTempPath(), "yt-dlp.exe");
            var finalPath = "/usr/bin/yt-dlp";

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockConfigurationService.Setup(x => x.GetDefaultTempDirectory()).Returns(Path.GetTempPath());
            _mockConfigurationService.Setup(x => x.ResolveUtilityPath("yt-dlp")).Returns(finalPath);
            _mockDownloadService.Setup(x => x.GetFileSizeAsync(downloadUrl)).ReturnsAsync(1024 * 1024);
            _mockDownloadService.Setup(x => x.DownloadFileAsync(downloadUrl, It.IsAny<string>(), It.IsAny<IProgress<DownloadProgress>>()))
                .ReturnsAsync(true);

            var progressReporter = new Progress<UpgradeProgress>();

            // Act
            var result = await _ytDlpUpgradeService.UpgradeAsync(progressReporter);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("yt-dlp upgraded successfully from 2023.01.01 to 2023.02.01", result.Message);
            _mockConfigurationService.Verify(x => x.SetUtilityPathAsync("yt-dlp", finalPath), Times.Once);
        }

        [Fact]
        public async Task UpgradeAsync_ShouldReturnFailure_WhenDownloadFails()
        {
            // Arrange
            var currentVersion = "2023.01.01";
            var latestVersion = "2023.02.01";
            var downloadUrl = "https://example.com/yt-dlp.exe";

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockConfigurationService.Setup(x => x.GetDefaultTempDirectory()).Returns(Path.GetTempPath());
            _mockDownloadService.Setup(x => x.GetFileSizeAsync(downloadUrl)).ReturnsAsync(1024 * 1024);
            _mockDownloadService.Setup(x => x.DownloadFileAsync(downloadUrl, It.IsAny<string>(), It.IsAny<IProgress<DownloadProgress>>()))
                .ReturnsAsync(false);

            var progressReporter = new Progress<UpgradeProgress>();

            // Act
            var result = await _ytDlpUpgradeService.UpgradeAsync(progressReporter);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Failed to download yt-dlp", result.Message);
        }

        [Fact]
        public async Task UpgradeAsync_ShouldReturnFailure_WhenVersionCheckFails()
        {
            // Arrange
            var currentVersion = string.Empty;
            var latestVersion = "2023.02.01";

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockVersionChecker.Setup(x => x.CompareVersionsAsync(currentVersion, latestVersion))
                .ReturnsAsync(VersionComparisonResult.ComparisonFailed);

            var progressReporter = new Progress<UpgradeProgress>();

            // Act
            var result = await _ytDlpUpgradeService.UpgradeAsync(progressReporter);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Failed to check for updates", result.Message);
        }

        [Fact]
        public void GetUtilityName_ShouldReturnYtDlp()
        {
            // Act
            var result = _ytDlpUpgradeService.GetUtilityName();

            // Assert
            Assert.Equal("yt-dlp", result);
        }

        [Fact]
        public async Task UpgradeAsync_ShouldHandleCancellation_WhenCancelled()
        {
            // Arrange
            var currentVersion = "2023.01.01";
            var latestVersion = "2023.02.01";
            var downloadUrl = "https://example.com/yt-dlp.exe";
            var cancellationToken = new CancellationTokenSource();
            var progressReporter = new Progress<UpgradeProgress>();

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockConfigurationService.Setup(x => x.GetDefaultTempDirectory()).Returns(Path.GetTempPath());
            _mockDownloadService.Setup(x => x.GetFileSizeAsync(downloadUrl)).ReturnsAsync(1024 * 1024);
            _mockDownloadService.Setup(x => x.DownloadFileAsync(downloadUrl, It.IsAny<string>(), It.IsAny<IProgress<DownloadProgress>>(), cancellationToken.Token))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _ytDlpUpgradeService.UpgradeAsync(progressReporter, cancellationToken.Token));
        }

        [Fact]
        public async Task UpgradeAsync_ShouldReturnUpToDate_WhenAlreadyUpToDate()
        {
            // Arrange
            var currentVersion = "2023.02.01";
            var latestVersion = "2023.02.01";

            _mockVersionChecker.Setup(x => x.GetCurrentVersionAsync()).ReturnsAsync(currentVersion);
            _mockVersionChecker.Setup(x => x.GetLatestVersionAsync()).ReturnsAsync(latestVersion);
            _mockVersionChecker.Setup(x => x.CompareVersionsAsync(currentVersion, latestVersion))
                .ReturnsAsync(VersionComparisonResult.UpToDate);

            var progressReporter = new Progress<UpgradeProgress>();

            // Act
            var result = await _ytDlpUpgradeService.UpgradeAsync(progressReporter);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Contains("already up to date", result.Message);
        }
    }
}