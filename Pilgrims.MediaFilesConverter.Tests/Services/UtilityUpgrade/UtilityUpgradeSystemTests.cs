using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pilgrims.MediaFilesConverter.Services.UtilityUpgrade;
using Xunit;
using Pilgrims.MediaFilesConverter.Models;

namespace Pilgrims.MediaFilesConverter.Tests.Services.UtilityUpgrade
{
    /// <summary>
    /// System tests for the complete utility upgrade system
    /// </summary>
    public class UtilityUpgradeSystemTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly string _testDirectory;

        public UtilityUpgradeSystemTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "UtilityUpgradeSystemTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);

            var services = new ServiceCollection();
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
            services.AddUtilityUpgradeServices();
            
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task SystemTest_CompleteWorkflow_ValidatesAllServicesRegistered()
        {
            // Arrange & Act
            var upgradeService = _serviceProvider.GetService<UtilityUpgradeService>();
            var configService = _serviceProvider.GetService<IUtilityConfigurationService>();
            var downloadService = _serviceProvider.GetService<IDownloadService>();
            var extractionService = _serviceProvider.GetService<IExtractionService>();
            var ffmpegVersionChecker = _serviceProvider.GetService<FFmpegVersionChecker>();
            var ytDlpVersionChecker = _serviceProvider.GetService<YtDlpVersionChecker>();
            var ffmpegUpgradeService = _serviceProvider.GetService<FFmpegUpgradeService>();
            var ytDlpUpgradeService = _serviceProvider.GetService<YtDlpUpgradeService>();

            // Assert
            Assert.NotNull(upgradeService);
            Assert.NotNull(configService);
            Assert.NotNull(downloadService);
            Assert.NotNull(extractionService);
            Assert.NotNull(ffmpegVersionChecker);
            Assert.NotNull(ytDlpVersionChecker);
            Assert.NotNull(ffmpegUpgradeService);
            Assert.NotNull(ytDlpUpgradeService);
        }

        [Fact]
        public async Task SystemTest_ConfigurationService_ValidatesPathOperations()
        {
            // Arrange
            var configService = _serviceProvider.GetRequiredService<IUtilityConfigurationService>();
            var ffmpegPath = Path.Combine(_testDirectory, "ffmpeg.exe");
            var ytDlpPath = Path.Combine(_testDirectory, "yt-dlp.exe");

            // Act
            await configService.SetFfmpegPathAsync(ffmpegPath);
            await configService.SetYtDlpPathAsync(ytDlpPath);
            
            var retrievedFfmpegPath = await configService.GetFfmpegPathAsync();
            var retrievedYtDlpPath = await configService.GetYtDlpPathAsync();

            // Assert
            Assert.Equal(ffmpegPath, retrievedFfmpegPath);
            Assert.Equal(ytDlpPath, retrievedYtDlpPath);
        }

        [Fact]
        public async Task SystemTest_UpgradeService_ValidatesUtilityInfoRetrieval()
        {
            // Arrange
            var upgradeService = _serviceProvider.GetRequiredService<UtilityUpgradeService>();
            var configService = _serviceProvider.GetRequiredService<IUtilityConfigurationService>();
            
            var ffmpegPath = Path.Combine(_testDirectory, "ffmpeg.exe");
            await File.WriteAllTextAsync(ffmpegPath, "mock ffmpeg content");
            await configService.SetFfmpegPathAsync(ffmpegPath);

            // Act
            var ffmpegInfo = await upgradeService.GetFfmpegInfoAsync();
            var ytDlpInfo = await upgradeService.GetYtDlpInfoAsync();

            // Assert
            Assert.NotNull(ffmpegInfo);
            Assert.NotNull(ytDlpInfo);
            Assert.Equal(ffmpegPath, ffmpegInfo.Path);
            Assert.True(ffmpegInfo.IsAvailable);
        }

        [Fact]
        public async Task SystemTest_ErrorHandling_ValidatesGracefulDegradation()
        {
            // Arrange
            var upgradeService = _serviceProvider.GetRequiredService<UtilityUpgradeService>();
            var configService = _serviceProvider.GetRequiredService<IUtilityConfigurationService>();
            
            // Set non-existent paths
            await configService.SetFfmpegPathAsync("nonexistent\\ffmpeg.exe");
            await configService.SetYtDlpPathAsync("nonexistent\\yt-dlp.exe");

            // Act
            var ffmpegInfo = await upgradeService.GetFfmpegInfoAsync();
            var ytDlpInfo = await upgradeService.GetYtDlpInfoAsync();
            
            var ffmpegUpdate = await upgradeService.CheckFfmpegUpdateAsync();
            var ytDlpUpdate = await upgradeService.CheckYtDlpUpdateAsync();

            // Assert
            Assert.NotNull(ffmpegInfo);
            Assert.NotNull(ytDlpInfo);
            Assert.False(ffmpegInfo.IsAvailable);
            Assert.False(ytDlpInfo.IsAvailable);
            Assert.False(ffmpegUpdate.UpdateAvailable);
            Assert.False(ytDlpUpdate.UpdateAvailable);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDirectory))
                    Directory.Delete(_testDirectory, true);
            }
            catch { }
            
            _serviceProvider?.Dispose();
        }
    }
}