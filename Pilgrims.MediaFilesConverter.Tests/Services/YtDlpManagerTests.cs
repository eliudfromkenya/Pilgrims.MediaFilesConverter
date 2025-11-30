using System;
using System.IO;
using System.Threading.Tasks;
using Pilgrims.MediaFilesConverter.Services;
using Xunit;

namespace Pilgrims.MediaFilesConverter.Tests.Services
{
    public class YtDlpManagerTests
    {
        [Fact]
        public void GetYtDlpPath_ShouldReturnPath_WhenAvailable()
        {
            // Act
            var result = YtDlpManager.Instance.GetYtDlpPath();

            // Assert
            // Should return path when yt-dlp is available (which it is on this system)
            Assert.NotNull(result);
            Assert.Contains("yt-dlp", result);
        }

        [Fact]
        public async Task CheckAvailabilityAsync_ShouldReturnAvailable_WhenFound()
        {
            // Act
            var result = await YtDlpManager.Instance.CheckAvailabilityAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsAvailable);
            Assert.NotNull(result.ExecutablePath);
            Assert.NotNull(result.Version);
            Assert.Null(result.ErrorMessage);
            Assert.Null(result.SuggestedAction);
        }

        [Fact]
        public void GetYtDlpDownloadUrl_ShouldReturnWindowsUrl_ForWindows()
        {
            // Act
            var method = typeof(YtDlpManager).GetMethod("GetYtDlpDownloadUrl", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var url = method?.Invoke(YtDlpManager.Instance, null) as string;

            // Assert
            if (OperatingSystem.IsWindows())
            {
                Assert.Contains("yt-dlp.exe", url);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                Assert.Equal("yt-dlp", url);
            }
        }

        [Fact]
        public void Constructor_ShouldSetCorrectPaths()
        {
            // Act
            var manager = YtDlpManager.Instance;

            // Assert
            Assert.NotNull(manager);
            // The singleton should be properly initialized
        }
    }
}