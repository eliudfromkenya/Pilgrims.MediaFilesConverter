using System;
using System.Threading.Tasks;
using Pilgrims.MediaFilesConverter.Services;

namespace TestYouTubeDownload
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Testing YouTube Download Functionality");
            Console.WriteLine("=====================================");

            // Test 1: Check yt-dlp availability
            Console.WriteLine("\n1. Testing yt-dlp availability...");
            var availability = await YouTubeDownloadService.Instance.GetYtDlpAvailabilityAsync();
            if (availability != null)
            {
                Console.WriteLine($"   IsAvailable: {availability.IsAvailable}");
                Console.WriteLine($"   ExecutablePath: {availability.ExecutablePath}");
                Console.WriteLine($"   Version: {availability.Version}");
                Console.WriteLine($"   ErrorMessage: {availability.ErrorMessage}");
                Console.WriteLine($"   SuggestedAction: {availability.SuggestedAction}");
            }
            else
            {
                Console.WriteLine("   ERROR: GetYtDlpAvailabilityAsync returned null!");
            }

            // Test 2: Test URL validation
            Console.WriteLine("\n2. Testing URL validation...");
            var testUrls = new[]
            {
                "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                "https://m.youtube.com/watch?v=dQw4w9WgXcQ",
                "https://youtu.be/dQw4w9WgXcQ",
                "https://www.youtube.com/shorts/1234567890",
                "https://www.invalid-url.com/video"
            };

            foreach (var url in testUrls)
            {
                var isValid = YouTubeDownloadService.Instance.IsValidYouTubeUrl(url);
                Console.WriteLine($"   {url}: {(isValid ? "VALID" : "INVALID")}");
            }

            // Test 3: Test video info retrieval
            Console.WriteLine("\n3. Testing video info retrieval...");
            var videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            try
            {
                var videoInfo = await YouTubeDownloadService.Instance.GetVideoInfoAsync(videoUrl);
                if (!string.IsNullOrEmpty(videoInfo))
                {
                    Console.WriteLine($"   Raw video info received (length: {videoInfo.Length} characters)");
                    Console.WriteLine($"   First 200 characters: {videoInfo.Substring(0, Math.Min(200, videoInfo.Length))}");
                }
                else
                {
                    Console.WriteLine("   ERROR: GetVideoInfoAsync returned null or empty!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ERROR: {ex.Message}");
            }

            // Test 4: Test download functionality (commented out by default)
            Console.WriteLine("\n4. Testing download functionality...");
            Console.WriteLine("   (This test is commented out to avoid downloading files during testing)");
            Console.WriteLine("   To enable, uncomment the code in Program.cs");
            
            /* Uncomment to test actual download
            try
            {
                var outputDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var progress = new Progress<DownloadProgress>(p => 
                {
                    Console.WriteLine($"   Progress: {p.PercentComplete}% - {p.Status}");
                });

                var mediaFile = await YouTubeDownloadService.Instance.DownloadVideoAsync(
                    videoUrl, 
                    outputDir, 
                    DownloadType.MP4, 
                    "best", 
                    false, 
                    progress
                );

                if (mediaFile != null)
                {
                    Console.WriteLine($"   Download successful!");
                    Console.WriteLine($"   File path: {mediaFile.FilePath}");
                    Console.WriteLine($"   File size: {mediaFile.FileSize} bytes");
                    
                    // Clean up
                    if (File.Exists(mediaFile.FilePath))
                    {
                        File.Delete(mediaFile.FilePath);
                        Console.WriteLine($"   Test file cleaned up");
                    }
                }
                else
                {
                    Console.WriteLine($"   Download failed!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Download error: {ex.Message}");
            }
            */

            Console.WriteLine("\n=====================================");
            Console.WriteLine("Testing completed!");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}