using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Pilgrims.MediaFilesConverter.Tests.Services.UtilityUpgrade
{
    /// <summary>
    /// Unit tests for UtilityConfigurationService
    /// </summary>
    public class UtilityConfigurationServiceTests
    {
        private readonly Mock<ILogger<UtilityConfigurationService>> _mockLogger;
        private readonly string _testConfigPath;
        private readonly UtilityConfigurationService _configurationService;

        public UtilityConfigurationServiceTests()
        {
            _mockLogger = new Mock<ILogger<UtilityConfigurationService>>();
            _testConfigPath = Path.Combine(Path.GetTempPath(), "test-utility-config.json");
            _configurationService = new UtilityConfigurationService(_mockLogger.Object, _testConfigPath);
        }

        public void Dispose()
        {
            if (File.Exists(_testConfigPath))
            {
                File.Delete(_testConfigPath);
            }
        }

        [Fact]
        public async Task GetUtilityPathAsync_ShouldReturnPath_WhenPathIsSet()
        {
            // Arrange
            var utilityName = "ffmpeg";
            var expectedPath = "/usr/bin/ffmpeg";
            await _configurationService.SetUtilityPathAsync(utilityName, expectedPath);

            // Act
            var result = await _configurationService.GetUtilityPathAsync(utilityName);

            // Assert
            Assert.Equal(expectedPath, result);
        }

        [Fact]
        public async Task GetUtilityPathAsync_ShouldReturnEmptyString_WhenPathIsNotSet()
        {
            // Arrange
            var utilityName = "non-existent-utility";

            // Act
            var result = await _configurationService.GetUtilityPathAsync(utilityName);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task SetUtilityPathAsync_ShouldSavePath_ToConfigurationFile()
        {
            // Arrange
            var utilityName = "ffmpeg";
            var expectedPath = "/usr/bin/ffmpeg";

            // Act
            await _configurationService.SetUtilityPathAsync(utilityName, expectedPath);

            // Assert
            var result = await _configurationService.GetUtilityPathAsync(utilityName);
            Assert.Equal(expectedPath, result);
            Assert.True(File.Exists(_testConfigPath));
        }

        [Fact]
        public void GetDefaultDownloadDirectory_ShouldReturnValidPath()
        {
            // Act
            var result = _configurationService.GetDefaultDownloadDirectory();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.True(Directory.Exists(result) || result.Contains("Downloads"));
        }

        [Fact]
        public void GetDefaultTempDirectory_ShouldReturnValidPath()
        {
            // Act
            var result = _configurationService.GetDefaultTempDirectory();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.True(Directory.Exists(result) || result.Contains("Temp"));
        }

        [Fact]
        public void GetOperationTimeout_ShouldReturnValidTimeout()
        {
            // Act
            var result = _configurationService.GetOperationTimeout();

            // Assert
            Assert.True(result > TimeSpan.Zero);
            Assert.True(result.TotalMinutes >= 1);
        }

        [Fact]
        public async Task ValidateUtilityPathAsync_ShouldReturnTrue_WhenPathIsValid()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            try
            {
                // Act
                var result = await _configurationService.ValidateUtilityPathAsync(tempFile);

                // Assert
                Assert.True(result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task ValidateUtilityPathAsync_ShouldReturnFalse_WhenPathIsInvalid()
        {
            // Arrange
            var invalidPath = "non-existent-file.txt";

            // Act
            var result = await _configurationService.ValidateUtilityPathAsync(invalidPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetPlatformSpecificExecutableName_ShouldReturnCorrectName_ForWindows()
        {
            // Arrange
            var utilityName = "ffmpeg";
            var expectedName = "ffmpeg.exe";

            // Act
            var result = _configurationService.GetPlatformSpecificExecutableName(utilityName);

            // Assert
            Assert.Equal(expectedName, result);
        }

        [Fact]
        public void GetPlatformSpecificExecutableName_ShouldReturnCorrectName_ForLinux()
        {
            // Arrange
            var utilityName = "ffmpeg";
            var expectedName = "ffmpeg";

            // Act
            var result = _configurationService.GetPlatformSpecificExecutableName(utilityName);

            // Assert
            Assert.Equal(expectedName, result);
        }

        [Fact]
        public void ResolveUtilityPath_ShouldReturnFullPath_WhenUtilityNameIsProvided()
        {
            // Arrange
            var utilityName = "ffmpeg";

            // Act
            var result = _configurationService.ResolveUtilityPath(utilityName);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(utilityName, result);
        }

        [Fact]
        public async Task ClearConfigurationCache_ShouldClearCachedConfiguration()
        {
            // Arrange
            var utilityName = "ffmpeg";
            var expectedPath = "/usr/bin/ffmpeg";
            await _configurationService.SetUtilityPathAsync(utilityName, expectedPath);
            
            // Verify it's cached
            var cachedResult = await _configurationService.GetUtilityPathAsync(utilityName);
            Assert.Equal(expectedPath, cachedResult);

            // Act
            await _configurationService.ClearConfigurationCacheAsync();

            // Assert
            // Configuration should be reloaded from file (which should be empty after dispose)
            var result = await _configurationService.GetUtilityPathAsync(utilityName);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task LoadConfigurationAsync_ShouldHandleCorruptedConfigFile()
        {
            // Arrange
            var corruptedContent = "invalid json content";
            await File.WriteAllTextAsync(_testConfigPath, corruptedContent);

            // Act
            var result = await _configurationService.GetUtilityPathAsync("any-utility");

            // Assert
            Assert.Equal(string.Empty, result);
            _mockLogger.VerifyLogErrorContains("Failed to load configuration");
        }
    }
}