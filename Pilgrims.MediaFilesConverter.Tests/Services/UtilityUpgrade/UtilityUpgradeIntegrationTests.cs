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
    public class UtilityUpgradeIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly string _testDirectory;

        public UtilityUpgradeIntegrationTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "UtilityUpgradeTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);

            var services = new ServiceCollection();
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
            services.AddUtilityUpgradeServices();
            
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task GetUtilityInfoAsync_ValidConfiguration_ReturnsInfo()
        {
            // Arrange
            var upgradeService = _serviceProvider.GetRequiredService<UtilityUpgradeService>();
            var configService = _serviceProvider.GetRequiredService<IUtilityConfigurationService>();
            
            var ffmpegPath = Path.Combine(_testDirectory, "ffmpeg.exe");
            await File.WriteAllTextAsync(ffmpegPath, "test content");
            await configService.SetFfmpegPathAsync(ffmpegPath);

            // Act
            var info = await upgradeService.GetFfmpegInfoAsync();

            // Assert
            Assert.NotNull(info);
            Assert.Equal(ffmpegPath, info.Path);
            Assert.True(info.IsAvailable);
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