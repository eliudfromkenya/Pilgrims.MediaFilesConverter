using System;
using Xunit;
using Pilgrims.MediaFilesConverter.Models;

namespace Pilgrims.MediaFilesConverter.Tests.Services.UtilityUpgrade
{
    /// <summary>
    /// Unit tests for UtilityModels classes
    /// </summary>
    public class UtilityModelsTests
    {
        #region UpgradeProgress Tests

        [Fact]
        public void UpgradeProgress_Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var progress = new UpgradeProgress();

            // Assert
            Assert.Equal(0, progress.Percentage);
            Assert.Equal(0, progress.BytesDownloaded);
            Assert.Equal(0, progress.TotalBytes);
            Assert.Equal(0, progress.FilesExtracted);
            Assert.Equal(0, progress.TotalFiles);
            Assert.Equal(OperationType.None, progress.CurrentOperation);
            Assert.Null(progress.ErrorMessage);
        }

        [Fact]
        public void UpgradeProgress_Constructor_ShouldInitializeWithProvidedValues()
        {
            // Arrange
            var percentage = 50;
            var bytesDownloaded = 1024;
            var totalBytes = 2048;
            var filesExtracted = 5;
            var totalFiles = 10;
            var currentOperation = OperationType.Downloading;
            var errorMessage = "Test error";

            // Act
            var progress = new UpgradeProgress(percentage, bytesDownloaded, totalBytes, filesExtracted, totalFiles, currentOperation, errorMessage);

            // Assert
            Assert.Equal(percentage, progress.Percentage);
            Assert.Equal(bytesDownloaded, progress.BytesDownloaded);
            Assert.Equal(totalBytes, progress.TotalBytes);
            Assert.Equal(filesExtracted, progress.FilesExtracted);
            Assert.Equal(totalFiles, progress.TotalFiles);
            Assert.Equal(currentOperation, progress.CurrentOperation);
            Assert.Equal(errorMessage, progress.ErrorMessage);
        }

        [Fact]
        public void UpgradeProgress_Percentage_ShouldBeClampedTo100()
        {
            // Arrange
            var progress = new UpgradeProgress();

            // Act
            progress.Percentage = 150;

            // Assert
            Assert.Equal(100, progress.Percentage);
        }

        [Fact]
        public void UpgradeProgress_Percentage_ShouldBeClampedTo0()
        {
            // Arrange
            var progress = new UpgradeProgress();

            // Act
            progress.Percentage = -10;

            // Assert
            Assert.Equal(0, progress.Percentage);
        }

        [Fact]
        public void UpgradeProgress_BytesDownloaded_ShouldNotExceedTotalBytes()
        {
            // Arrange
            var progress = new UpgradeProgress();
            progress.TotalBytes = 1000;

            // Act
            progress.BytesDownloaded = 1500;

            // Assert
            Assert.Equal(1000, progress.BytesDownloaded);
        }

        [Fact]
        public void UpgradeProgress_FilesExtracted_ShouldNotExceedTotalFiles()
        {
            // Arrange
            var progress = new UpgradeProgress();
            progress.TotalFiles = 10;

            // Act
            progress.FilesExtracted = 15;

            // Assert
            Assert.Equal(10, progress.FilesExtracted);
        }

        #endregion

        #region UpgradeResult Tests

        [Fact]
        public void UpgradeResult_Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var result = new UpgradeResult();

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(DateTime.MinValue, result.CompletedAt);
        }

        [Fact]
        public void UpgradeResult_Constructor_ShouldInitializeWithProvidedValues()
        {
            // Arrange
            var success = true;
            var errorMessage = "Test error";
            var completedAt = DateTime.UtcNow;

            // Act
            var result = new UpgradeResult(success, errorMessage, completedAt);

            // Assert
            Assert.Equal(success, result.Success);
            Assert.Equal(errorMessage, result.ErrorMessage);
            Assert.Equal(completedAt, result.CompletedAt);
        }

        [Fact]
        public void UpgradeResult_SuccessfulResult_ShouldHaveSuccessTrue()
        {
            // Arrange & Act
            var result = UpgradeResult.SuccessfulResult();

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
            Assert.NotEqual(DateTime.MinValue, result.CompletedAt);
        }

        [Fact]
        public void UpgradeResult_FailedResult_ShouldHaveSuccessFalse()
        {
            // Arrange
            var errorMessage = "Test error";

            // Act
            var result = UpgradeResult.FailedResult(errorMessage);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(errorMessage, result.ErrorMessage);
            Assert.NotEqual(DateTime.MinValue, result.CompletedAt);
        }

        [Fact]
        public void UpgradeResult_FailedResult_ShouldUseDefaultErrorMessage()
        {
            // Act
            var result = UpgradeResult.FailedResult(null);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Unknown error", result.ErrorMessage);
        }

        #endregion

        #region UpdateCheckResult Tests

        [Fact]
        public void UpdateCheckResult_Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var result = new UpdateCheckResult();

            // Assert
            Assert.False(result.IsUpdateAvailable);
            Assert.Null(result.CurrentVersion);
            Assert.Null(result.LatestVersion);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void UpdateCheckResult_Constructor_ShouldInitializeWithProvidedValues()
        {
            // Arrange
            var isUpdateAvailable = true;
            var currentVersion = "1.2.3";
            var latestVersion = "1.2.4";
            var errorMessage = "Test error";

            // Act
            var result = new UpdateCheckResult(isUpdateAvailable, currentVersion, latestVersion, errorMessage);

            // Assert
            Assert.Equal(isUpdateAvailable, result.IsUpdateAvailable);
            Assert.Equal(currentVersion, result.CurrentVersion);
            Assert.Equal(latestVersion, result.LatestVersion);
            Assert.Equal(errorMessage, result.ErrorMessage);
        }

        [Fact]
        public void UpdateCheckResult_SuccessfulResult_ShouldHaveCorrectValues()
        {
            // Arrange
            var currentVersion = "1.2.3";
            var latestVersion = "1.2.4";

            // Act
            var result = UpdateCheckResult.SuccessfulResult(currentVersion, latestVersion);

            // Assert
            Assert.True(result.IsUpdateAvailable);
            Assert.Equal(currentVersion, result.CurrentVersion);
            Assert.Equal(latestVersion, result.LatestVersion);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void UpdateCheckResult_FailedResult_ShouldHaveCorrectValues()
        {
            // Arrange
            var errorMessage = "Test error";

            // Act
            var result = UpdateCheckResult.FailedResult(errorMessage);

            // Assert
            Assert.False(result.IsUpdateAvailable);
            Assert.Null(result.CurrentVersion);
            Assert.Null(result.LatestVersion);
            Assert.Equal(errorMessage, result.ErrorMessage);
        }

        #endregion

        #region UtilityInfo Tests

        [Fact]
        public void UtilityInfo_Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var info = new UtilityInfo();

            // Assert
            Assert.Null(info.Name);
            Assert.Null(info.CurrentVersion);
            Assert.Null(info.LatestVersion);
            Assert.Null(info.ExecutablePath);
            Assert.False(info.IsAvailable);
            Assert.False(info.IsUpdateAvailable);
            Assert.Equal(UpdateStatus.Unknown, info.UpdateStatus);
            Assert.Null(info.ErrorMessage);
        }

        [Fact]
        public void UtilityInfo_Constructor_ShouldInitializeWithProvidedValues()
        {
            // Arrange
            var name = "ffmpeg";
            var currentVersion = "1.2.3";
            var latestVersion = "1.2.4";
            var executablePath = "/usr/bin/ffmpeg";
            var isAvailable = true;
            var isUpdateAvailable = true;
            var updateStatus = UpdateStatus.UpdateAvailable;
            var errorMessage = "Test error";

            // Act
            var info = new UtilityInfo(name, currentVersion, latestVersion, executablePath, isAvailable, isUpdateAvailable, updateStatus, errorMessage);

            // Assert
            Assert.Equal(name, info.Name);
            Assert.Equal(currentVersion, info.CurrentVersion);
            Assert.Equal(latestVersion, info.LatestVersion);
            Assert.Equal(executablePath, info.ExecutablePath);
            Assert.Equal(isAvailable, info.IsAvailable);
            Assert.Equal(isUpdateAvailable, info.IsUpdateAvailable);
            Assert.Equal(updateStatus, info.UpdateStatus);
            Assert.Equal(errorMessage, info.ErrorMessage);
        }

        [Fact]
        public void UtilityInfo_AvailableUtility_ShouldHaveCorrectValues()
        {
            // Arrange
            var name = "ffmpeg";
            var currentVersion = "1.2.3";
            var latestVersion = "1.2.4";
            var executablePath = "/usr/bin/ffmpeg";

            // Act
            var info = UtilityInfo.AvailableUtility(name, currentVersion, latestVersion, executablePath);

            // Assert
            Assert.Equal(name, info.Name);
            Assert.Equal(currentVersion, info.CurrentVersion);
            Assert.Equal(latestVersion, info.LatestVersion);
            Assert.Equal(executablePath, info.ExecutablePath);
            Assert.True(info.IsAvailable);
            Assert.True(info.IsUpdateAvailable);
            Assert.Equal(UpdateStatus.UpdateAvailable, info.UpdateStatus);
            Assert.Null(info.ErrorMessage);
        }

        [Fact]
        public void UtilityInfo_NotAvailableUtility_ShouldHaveCorrectValues()
        {
            // Arrange
            var name = "ffmpeg";
            var latestVersion = "1.2.4";

            // Act
            var info = UtilityInfo.NotAvailableUtility(name, latestVersion);

            // Assert
            Assert.Equal(name, info.Name);
            Assert.Null(info.CurrentVersion);
            Assert.Equal(latestVersion, info.LatestVersion);
            Assert.Null(info.ExecutablePath);
            Assert.False(info.IsAvailable);
            Assert.False(info.IsUpdateAvailable);
            Assert.Equal(UpdateStatus.NotInstalled, info.UpdateStatus);
            Assert.Null(info.ErrorMessage);
        }

        [Fact]
        public void UtilityInfo_ErrorUtility_ShouldHaveCorrectValues()
        {
            // Arrange
            var name = "ffmpeg";
            var errorMessage = "Test error";

            // Act
            var info = UtilityInfo.ErrorUtility(name, errorMessage);

            // Assert
            Assert.Equal(name, info.Name);
            Assert.Null(info.CurrentVersion);
            Assert.Null(info.LatestVersion);
            Assert.Null(info.ExecutablePath);
            Assert.False(info.IsAvailable);
            Assert.False(info.IsUpdateAvailable);
            Assert.Equal(UpdateStatus.Error, info.UpdateStatus);
            Assert.Equal(errorMessage, info.ErrorMessage);
        }

        #endregion
    }
}