using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Pilgrims.MediaFilesConverter.Models;

namespace Pilgrims.MediaFilesConverter.Services
{
    public class YouTubeDownloadService
    {
        private static readonly Lazy<YouTubeDownloadService> _instance = new(() => new YouTubeDownloadService());
        public static YouTubeDownloadService Instance => _instance.Value;

        private readonly YtDlpManager _ytDlpManager;

        private YouTubeDownloadService() 
        {
            _ytDlpManager = YtDlpManager.Instance;
        }

        /// <summary>
        /// Validates if the provided URL is a valid YouTube URL
        /// </summary>
        public bool IsValidYouTubeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            var youtubeRegex = new Regex(
                @"^(https?://)?(www\.|m\.)?(youtube\.com/(watch\?v=|embed/|v/)|youtu\.be/)[\w\-_]{11}(&.*)?$",
                RegexOptions.IgnoreCase);

            return youtubeRegex.IsMatch(url);
        }

        /// <summary>
        /// Downloads a video or audio from YouTube using yt-dlp
        /// </summary>
        public async Task<MediaFile?> DownloadVideoAsync(string youtubeUrl, string outputDirectory, string format = "mp4", string quality = "720p", bool audioOnly = false, IProgress<string>? progress = null)
        {
            if (!IsValidYouTubeUrl(youtubeUrl))
            {
                progress?.Report("Invalid YouTube URL");
                return null;
            }

            try
            {
                progress?.Report("Starting download...");

                // Ensure output directory exists
                Directory.CreateDirectory(outputDirectory);

                // Create a unique filename template
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var outputTemplate = Path.Combine(outputDirectory, $"youtube_{timestamp}_%(title)s.%(ext)s");

                // Build format selector based on quality, format, and audio-only flag
                string formatSelector = BuildFormatSelector(format, quality, audioOnly);

                // Prepare yt-dlp command with audio extraction if needed
                var arguments = audioOnly 
                    ? $"--extract-audio --audio-format {format} --audio-quality {quality} --output \"{outputTemplate}\" --no-playlist \"{youtubeUrl}\""
                    : $"--format \"{formatSelector}\" --output \"{outputTemplate}\" --no-playlist \"{youtubeUrl}\"";

                var ytDlpPath = _ytDlpManager.GetYtDlpPath();
                if (string.IsNullOrEmpty(ytDlpPath))
                {
                    progress?.Report("yt-dlp executable not found. Please download it from https://github.com/yt-dlp/yt-dlp/releases");
                    return null;
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var downloadType = audioOnly ? "audio" : "video";
                var qualityInfo = audioOnly ? $"{quality} {format.ToUpper()}" : $"{quality} {format.ToUpper()}";
                progress?.Report($"Downloading {downloadType} in {qualityInfo}...");

                using var process = new Process { StartInfo = processInfo };
                
                var outputData = string.Empty;
                var errorData = string.Empty;

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        outputData += e.Data + Environment.NewLine;
                        progress?.Report($"Download: {e.Data}");
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        errorData += e.Data + Environment.NewLine;
                        progress?.Report($"Info: {e.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    progress?.Report("Download completed successfully!");
                    
                    // Find the downloaded file
                    var downloadedFile = FindDownloadedFile(outputDirectory, timestamp);
                    if (downloadedFile != null)
                    {
                        return new MediaFile(downloadedFile);
                    }
                }
                else
                {
                    progress?.Report($"Download failed: {errorData}");
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"Error: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Checks if yt-dlp is available on the system
        /// </summary>
        public async Task<bool> IsYtDlpAvailableAsync()
        {
            var result = await _ytDlpManager.CheckAvailabilityAsync();
            return result.IsAvailable;
        }

        /// <summary>
        /// Gets detailed information about yt-dlp availability
        /// </summary>
        public async Task<YtDlpAvailabilityResult> GetYtDlpAvailabilityAsync()
        {
            return await _ytDlpManager.CheckAvailabilityAsync();
        }

        /// <summary>
        /// Gets video information without downloading
        /// </summary>
        public async Task<string?> GetVideoInfoAsync(string youtubeUrl)
        {
            if (!IsValidYouTubeUrl(youtubeUrl))
                return null;

            try
            {
                var ytDlpPath = _ytDlpManager.GetYtDlpPath();
                if (string.IsNullOrEmpty(ytDlpPath))
                    return null;

                var processInfo = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = $"--get-title --get-duration \"{youtubeUrl}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                return process.ExitCode == 0 ? output.Trim() : null;
            }
            catch
            {
                return null;
            }
        }

        private string? FindDownloadedFile(string directory, string timestamp)
        {
            try
            {
                var files = Directory.GetFiles(directory, $"youtube_{timestamp}_*");
                return files.Length > 0 ? files[0] : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Builds the format selector string for yt-dlp based on quality and format preferences
        /// </summary>
        private string BuildFormatSelector(string format, string quality, bool audioOnly)
        {
            // Handle special quality cases
            if (quality.Equals("best", StringComparison.OrdinalIgnoreCase))
            {
                return format.Equals("mp4", StringComparison.OrdinalIgnoreCase) 
                    ? "best[ext=mp4]/best" 
                    : $"best[ext={format}]/best";
            }
            
            if (quality.Equals("worst", StringComparison.OrdinalIgnoreCase))
            {
                return format.Equals("mp4", StringComparison.OrdinalIgnoreCase) 
                    ? "worst[ext=mp4]/worst" 
                    : $"worst[ext={format}]/worst";
            }

            // Extract height from quality (e.g., "720p" -> "720")
            var heightMatch = Regex.Match(quality, @"(\d+)p?");
            if (heightMatch.Success)
            {
                var height = heightMatch.Groups[1].Value;
                
                // Build format selector with height and format preferences
                if (format.Equals("mp4", StringComparison.OrdinalIgnoreCase))
                {
                    return $"best[height<={height}][ext=mp4]/best[height<={height}]/best[ext=mp4]/best";
                }
                else
                {
                    return $"best[height<={height}][ext={format}]/best[height<={height}]/best[ext={format}]/best";
                }
            }

            // Fallback to best quality with format preference
            return format.Equals("mp4", StringComparison.OrdinalIgnoreCase) 
                ? "best[ext=mp4]/best" 
                : $"best[ext={format}]/best";
        }
    }
}