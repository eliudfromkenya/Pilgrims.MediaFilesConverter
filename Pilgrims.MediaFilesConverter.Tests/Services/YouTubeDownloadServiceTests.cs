using System;
using System.Threading.Tasks;
using Pilgrims.MediaFilesConverter.Models;
using Pilgrims.MediaFilesConverter.Services;
using Xunit;

namespace Pilgrims.MediaFilesConverter.Tests.Services
{
    public class YouTubeDownloadServiceTests
    {
        private readonly YouTubeDownloadService _service;

        public YouTubeDownloadServiceTests()
        {
            _service = YouTubeDownloadService.Instance;
        }

        [Fact]
        public void IsValidYouTubeUrl_ShouldReturnTrue_ForValidUrls()
        {
            // Arrange
            var validUrls = new[]
            {
                "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                "https://youtu.be/dQw4w9WgXcQ",
                "https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PLrAXtmErZgOeiKm4sgNOknGvNjby9efdf",
                "https://m.youtube.com/watch?v=dQw4w9WgXcQ"
            };

            // Act & Assert
            foreach (var url in validUrls)
            {
                Assert.True(_service.IsValidYouTubeUrl(url), $"URL should be valid: {url}");
            }
        }

        [Fact]
        public void IsValidYouTubeUrl_ShouldReturnFalse_ForInvalidUrls()
        {
            // Arrange
            var invalidUrls = new[]
            {
                "https://www.google.com",
                "not-a-url",
                "https://vimeo.com/123456",
                ""
            };

            // Act & Assert
            foreach (var url in invalidUrls)
            {
                Assert.False(_service.IsValidYouTubeUrl(url), $"URL should be invalid: {url}");
            }
        }

        [Fact]
        public async Task GetVideoInfoAsync_ShouldReturnVideoInfo_WhenYtDlpAvailable()
        {
            // Arrange
            var testUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

            // Act
            var result = await _service.GetVideoInfoAsync(testUrl);

            // Assert
            // Should return video info when yt-dlp is available (which it is on this system)
            Assert.NotNull(result);
            Assert.Contains("Rick Astley - Never Gonna Give You Up (Official Video)", result);
        }

        [Fact]
        public async Task DownloadVideoAsync_ShouldReturnMediaFile_WhenYtDlpAvailable()
        {
            // Arrange
            var testUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            var outputDirectory = Path.GetTempPath();
            var progress = new Progress<string>();

            // Act
            var result = await _service.DownloadVideoAsync(testUrl, outputDirectory, "mp4", "720p", false, progress);

            // Assert
            // Should return MediaFile when yt-dlp is available (which it is on this system)
            Assert.NotNull(result);
            Assert.Equal("youtube_", result.FileName.Substring(0, 8)); // Should start with youtube_ timestamp
            Assert.Equal(".mp4", result.FileExtension);
            
            // Clean up the downloaded file if it exists
            if (!string.IsNullOrEmpty(result.FilePath) && File.Exists(result.FilePath))
            {
                File.Delete(result.FilePath);
            }
        }

        [Fact]
        public async Task GetYtDlpAvailabilityAsync_ShouldReturnDetailedResult()
        {
            // Act
            var result = await _service.GetYtDlpAvailabilityAsync();

            // Assert
            Assert.NotNull(result);
            
            // If yt-dlp is available (which it is on this system), check that properties are set correctly
            if (result.IsAvailable)
            {
                Assert.NotNull(result.ExecutablePath);
                Assert.NotNull(result.Version);
                Assert.Null(result.ErrorMessage);
                Assert.Null(result.SuggestedAction);
            }
            else
            {
                // When yt-dlp is not available
                Assert.NotNull(result.ErrorMessage);
                Assert.NotNull(result.SuggestedAction);
            }
        }
    }
}