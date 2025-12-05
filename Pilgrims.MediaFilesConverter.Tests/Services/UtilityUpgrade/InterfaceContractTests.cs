using System;
using System.Threading.Tasks;
using Xunit;
using Pilgrims.MediaFilesConverter.Models;

namespace Pilgrims.MediaFilesConverter.Tests.Services.UtilityUpgrade
{
    /// <summary>
    /// Unit tests for IUtilityConfigurationService interface contract
    /// </summary>
    public class IUtilityConfigurationServiceTests
    {
        [Fact]
        public void IUtilityConfigurationService_ShouldDefineRequiredMethods()
        {
            // This test ensures the interface has the expected contract
            // In a real implementation, this would be tested through the concrete class
            
            // Arrange
            var interfaceType = typeof(IUtilityConfigurationService);
            
            // Act & Assert
            Assert.NotNull(interfaceType.GetMethod("GetUtilityPath"));
            Assert.NotNull(interfaceType.GetMethod("SetUtilityPath"));
            Assert.NotNull(interfaceType.GetMethod("GetDefaultDirectory"));
            Assert.NotNull(interfaceType.GetMethod("ValidateUtilityPath"));
        }

        [Fact]
        public void IUtilityConfigurationService_ShouldDefineRequiredProperties()
        {
            // Arrange
            var interfaceType = typeof(IUtilityConfigurationService);
            
            // Act & Assert
            Assert.NotNull(interfaceType.GetProperty("FfmpegPath"));
            Assert.NotNull(interfaceType.GetProperty("YtDlpPath"));
        }
    }

    /// <summary>
    /// Unit tests for IDownloadService interface contract
    /// </summary>
    public class IDownloadServiceTests
    {
        [Fact]
        public void IDownloadService_ShouldDefineRequiredMethods()
        {
            // Arrange
            var interfaceType = typeof(IDownloadService);
            
            // Act & Assert
            Assert.NotNull(interfaceType.GetMethod("DownloadFileAsync"));
            Assert.NotNull(interfaceType.GetMethod("GetFileSizeAsync"));
            Assert.NotNull(interfaceType.GetMethod("ValidateDownloadedFile"));
        }
    }

    /// <summary>
    /// Unit tests for IExtractionService interface contract
    /// </summary>
    public class IExtractionServiceTests
    {
        [Fact]
        public void IExtractionService_ShouldDefineRequiredMethods()
        {
            // Arrange
            var interfaceType = typeof(IExtractionService);
            
            // Act & Assert
            Assert.NotNull(interfaceType.GetMethod("ExtractArchiveAsync"));
            Assert.NotNull(interfaceType.GetMethod("IsSupportedArchive"));
            Assert.NotNull(interfaceType.GetMethod("GetExtractionProgress"));
        }
    }

    /// <summary>
    /// Unit tests for IVersionChecker interface contract
    /// </summary>
    public class IVersionCheckerTests
    {
        [Fact]
        public void IVersionChecker_ShouldDefineRequiredMethods()
        {
            // Arrange
            var interfaceType = typeof(IVersionChecker);
            
            // Act & Assert
            Assert.NotNull(interfaceType.GetMethod("GetCurrentVersion"));
            Assert.NotNull(interfaceType.GetMethod("GetLatestVersion"));
            Assert.NotNull(interfaceType.GetMethod("IsUpdateAvailable"));
        }
    }

    /// <summary>
    /// Unit tests for IUtilityUpgradeService interface contract
    /// </summary>
    public class IUtilityUpgradeServiceTests
    {
        [Fact]
        public void IUtilityUpgradeService_ShouldDefineRequiredMethods()
        {
            // Arrange
            var interfaceType = typeof(IUtilityUpgradeService);
            
            // Act & Assert
            Assert.NotNull(interfaceType.GetMethod("GetUtilityInfoAsync"));
            Assert.NotNull(interfaceType.GetMethod("CheckForUpdatesAsync"));
            Assert.NotNull(interfaceType.GetMethod("UpgradeAsync"));
        }

        [Fact]
        public void IUtilityUpgradeService_ShouldDefineRequiredProperty()
        {
            // Arrange
            var interfaceType = typeof(IUtilityUpgradeService);
            
            // Act & Assert
            Assert.NotNull(interfaceType.GetProperty("UtilityName"));
        }
    }
}