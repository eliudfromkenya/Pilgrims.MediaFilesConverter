using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Pilgrims.MediaFilesConverter.Services.UtilityUpgrade;
using Pilgrims.MediaFilesConverter.ViewModels;
using Xunit;
using Pilgrims.MediaFilesConverter.Models;

namespace Pilgrims.MediaFilesConverter.Tests.ViewModels
{
    public class UtilityUpgradeViewModelTests
    {
        private readonly Mock<UtilityUpgradeService> _upgradeServiceMock;
        private readonly Mock<ILogger<UtilityUpgradeViewModel>> _loggerMock;
        private readonly UtilityUpgradeViewModel _viewModel;

        public UtilityUpgradeViewModelTests()
        {
            _upgradeServiceMock = new Mock<UtilityUpgradeService>();
            _loggerMock = new Mock<ILogger<UtilityUpgradeViewModel>>();
            _viewModel = new UtilityUpgradeViewModel(_upgradeServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void Constructor_WithNullService_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new UtilityUpgradeViewModel(null, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new UtilityUpgradeViewModel(_upgradeServiceMock.Object, null));
        }

        [Fact]
        public async Task InitializeAsync_ServiceThrowsException_LogsError()
        {
            // Arrange
            _upgradeServiceMock
                .Setup(x => x.GetFfmpegInfoAsync())
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act
            await _viewModel.InitializeAsync();

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Test exception")),
                    It.IsAny<InvalidOperationException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CheckFfmpegUpdateAsync_ServiceThrowsException_LogsError()
        {
            // Arrange
            _upgradeServiceMock
                .Setup(x => x.CheckFfmpegUpdateAsync())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            await _viewModel.CheckFfmpegUpdateAsync();

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Network error")),
                    It.IsAny<HttpRequestException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task UpgradeFfmpegAsync_ServiceThrowsException_LogsError()
        {
            // Arrange
            _upgradeServiceMock
                .Setup(x => x.UpgradeFfmpegAsync(It.IsAny<IProgress<UpgradeProgress>>()))
                .ThrowsAsync(new InvalidOperationException("Upgrade failed"));

            // Act
            await _viewModel.UpgradeFfmpegAsync();

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Upgrade failed")),
                    It.IsAny<InvalidOperationException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void UpdateFfmpegUI_WithNullInfo_UpdatesPropertiesCorrectly()
        {
            // Arrange
            var info = new UtilityInfo(null, null, false);

            // Act
            _viewModel.UpdateFfmpegUI(info);

            // Assert
            Assert.Equal("Unknown", _viewModel.FFmpegVersion);
            Assert.Equal("Not configured", _viewModel.FFmpegPath);
            Assert.Equal("Not available", _viewModel.FFmpegStatus);
        }

        [Fact]
        public void UpdateYtDlpUI_WithNullInfo_UpdatesPropertiesCorrectly()
        {
            // Arrange
            var info = new UtilityInfo(null, null, false);

            // Act
            _viewModel.UpdateYtDlpUI(info);

            // Assert
            Assert.Equal("Unknown", _viewModel.YtDlpVersion);
            Assert.Equal("Not configured", _viewModel.YtDlpPath);
            Assert.Equal("Not available", _viewModel.YtDlpStatus);
        }
    }
}