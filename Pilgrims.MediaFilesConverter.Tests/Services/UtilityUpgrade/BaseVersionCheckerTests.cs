using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Pilgrims.MediaFilesConverter.Tests.Services.UtilityUpgrade
{
    /// <summary>
    /// Unit tests for BaseVersionChecker
    /// </summary>
    public class BaseVersionCheckerTests
    {
        private readonly Mock<ILogger<BaseVersionChecker>> _mockLogger;
        private readonly TestVersionChecker _versionChecker;

        public BaseVersionCheckerTests()
        {
            _mockLogger = new Mock<ILogger<BaseVersionChecker>>();
            _versionChecker = new TestVersionChecker(_mockLogger.Object);
        }

        [Fact]
        public async Task GetCurrentVersionAsync_ShouldReturnNull_WhenExecutablePathIsNull()
        {
            // Arrange
            string? executablePath = null;

            // Act
            var result = await _versionChecker.GetCurrentVersionAsync(executablePath);

            // Assert
            Assert.Null(result);
            _mockLogger.VerifyLogWarning("Executable path is invalid or file does not exist: {ExecutablePath}", Times.Once());
        }

        [Fact]
        public async Task GetCurrentVersionAsync_ShouldReturnNull_WhenExecutablePathIsEmpty()
        {
            // Arrange
            var executablePath = string.Empty;

            // Act
            var result = await _versionChecker.GetCurrentVersionAsync(executablePath);

            // Assert
            Assert.Null(result);
            _mockLogger.VerifyLogWarning("Executable path is invalid or file does not exist: {ExecutablePath}", Times.Once());
        }

        [Fact]
        public async Task GetCurrentVersionAsync_ShouldReturnNull_WhenExecutablePathIsWhitespace()
        {
            // Arrange
            var executablePath = "   ";

            // Act
            var result = await _versionChecker.GetCurrentVersionAsync(executablePath);

            // Assert
            Assert.Null(result);
            _mockLogger.VerifyLogWarning("Executable path is invalid or file does not exist: {ExecutablePath}", Times.Once());
        }

        [Fact]
        public async Task GetCurrentVersionAsync_ShouldReturnNull_WhenFileDoesNotExist()
        {
            // Arrange
            var executablePath = "nonexistent.exe";

            // Act
            var result = await _versionChecker.GetCurrentVersionAsync(executablePath);

            // Assert
            Assert.Null(result);
            _mockLogger.VerifyLogWarning("Executable path is invalid or file does not exist: {ExecutablePath}", Times.Once());
        }

        [Fact]
        public async Task GetCurrentVersionAsync_ShouldReturnVersion_WhenFileExistsAndVersionCommandSucceeds()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var expectedVersion = "1.2.3";
            _versionChecker.SetExpectedVersion(expectedVersion);

            try
            {
                // Act
                var result = await _versionChecker.GetCurrentVersionAsync(tempFile);

                // Assert
                Assert.Equal(expectedVersion, result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task GetCurrentVersionAsync_ShouldReturnNull_WhenVersionCommandThrowsException()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            _versionChecker.SetThrowException(true);

            try
            {
                // Act
                var result = await _versionChecker.GetCurrentVersionAsync(tempFile);

                // Assert
                Assert.Null(result);
                _mockLogger.VerifyLogError(Times.Once(), "Error getting current version for utility at {ExecutablePath}");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void CompareVersions_ShouldReturnUpToDate_WhenVersionsAreEqual()
        {
            // Arrange
            var currentVersion = "1.2.3";
            var latestVersion = "1.2.3";

            // Act
            var result = _versionChecker.CompareVersions(currentVersion, latestVersion);

            // Assert
            Assert.Equal(VersionComparisonResult.UpToDate, result);
        }

        [Fact]
        public void CompareVersions_ShouldReturnUpdateAvailable_WhenLatestIsNewer()
        {
            // Arrange
            var currentVersion = "1.2.3";
            var latestVersion = "1.2.4";

            // Act
            var result = _versionChecker.CompareVersions(currentVersion, latestVersion);

            // Assert
            Assert.Equal(VersionComparisonResult.UpdateAvailable, result);
        }

        [Fact]
        public void CompareVersions_ShouldReturnNewerThanLatest_WhenCurrentIsNewer()
        {
            // Arrange
            var currentVersion = "1.2.4";
            var latestVersion = "1.2.3";

            // Act
            var result = _versionChecker.CompareVersions(currentVersion, latestVersion);

            // Assert
            Assert.Equal(VersionComparisonResult.NewerThanLatest, result);
        }

        [Fact]
        public void CompareVersions_ShouldReturnComparisonFailed_WhenCurrentVersionIsNull()
        {
            // Arrange
            string? currentVersion = null;
            var latestVersion = "1.2.3";

            // Act
            var result = _versionChecker.CompareVersions(currentVersion, latestVersion);

            // Assert
            Assert.Equal(VersionComparisonResult.ComparisonFailed, result);
        }

        [Fact]
        public void CompareVersions_ShouldReturnComparisonFailed_WhenLatestVersionIsNull()
        {
            // Arrange
            var currentVersion = "1.2.3";
            string? latestVersion = null;

            // Act
            var result = _versionChecker.CompareVersions(currentVersion, latestVersion);

            // Assert
            Assert.Equal(VersionComparisonResult.ComparisonFailed, result);
        }

        [Fact]
        public void CompareVersions_ShouldReturnComparisonFailed_WhenBothVersionsAreNull()
        {
            // Arrange
            string? currentVersion = null;
            string? latestVersion = null;

            // Act
            var result = _versionChecker.CompareVersions(currentVersion, latestVersion);

            // Assert
            Assert.Equal(VersionComparisonResult.ComparisonFailed, result);
        }

        [Fact]
        public void CompareVersions_ShouldReturnComparisonFailed_WhenVersionsAreInvalid()
        {
            // Arrange
            var currentVersion = "invalid";
            var latestVersion = "1.2.3";

            // Act
            var result = _versionChecker.CompareVersions(currentVersion, latestVersion);

            // Assert
            Assert.Equal(VersionComparisonResult.ComparisonFailed, result);
        }

        [Theory]
        [InlineData("1.2.3", "1.2.4", VersionComparisonResult.UpdateAvailable)]
        [InlineData("1.2.3", "1.3.0", VersionComparisonResult.UpdateAvailable)]
        [InlineData("1.2.3", "2.0.0", VersionComparisonResult.UpdateAvailable)]
        [InlineData("1.2.3", "1.2.3", VersionComparisonResult.UpToDate)]
        [InlineData("1.2.4", "1.2.3", VersionComparisonResult.NewerThanLatest)]
        [InlineData("1.3.0", "1.2.3", VersionComparisonResult.NewerThanLatest)]
        [InlineData("2.0.0", "1.2.3", VersionComparisonResult.NewerThanLatest)]
        public void CompareVersions_ShouldReturnCorrectResult_ForVariousVersionComparisons(
            string currentVersion, string latestVersion, VersionComparisonResult expectedResult)
        {
            // Act
            var result = _versionChecker.CompareVersions(currentVersion, latestVersion);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        /// <summary>
        /// Test implementation of BaseVersionChecker for testing purposes
        /// </summary>
        private class TestVersionChecker : BaseVersionChecker
        {
            private string? _expectedVersion;
            private bool _throwException;

            public TestVersionChecker(ILogger<BaseVersionChecker> logger) : base(logger)
            {
            }

            public void SetExpectedVersion(string? version)
            {
                _expectedVersion = version;
            }

            public void SetThrowException(bool throwException)
            {
                _throwException = throwException;
            }

            public override Task<string?> GetLatestVersionAsync()
            {
                return Task.FromResult<string?>("2.0.0");
            }

            protected override Task<string?> ExecuteVersionCommandAsync(string executablePath)
            {
                if (_throwException)
                {
                    throw new InvalidOperationException("Test exception");
                }

                return Task.FromResult(_expectedVersion);
            }
        }
    }
}