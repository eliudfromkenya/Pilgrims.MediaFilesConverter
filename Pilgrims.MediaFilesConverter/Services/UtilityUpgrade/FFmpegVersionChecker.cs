using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Pilgrims.MediaFilesConverter.Services.UtilityUpgrade
{
    /// <summary>
    /// FFmpeg version checker implementation
    /// </summary>
    public class FFmpegVersionChecker : BaseVersionChecker
    {
        private readonly HttpClient _httpClient;
        private const string FFmpegReleaseUrl = "https://api.github.com/repos/FFmpeg/FFmpeg/releases/latest";

        public FFmpegVersionChecker(HttpClient httpClient, ILogger<FFmpegVersionChecker> logger) 
            : base(logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Gets the latest available version from FFmpeg GitHub releases
        /// </summary>
        public override async Task<string?> GetLatestVersionAsync()
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, FFmpegReleaseUrl);
                request.Headers.Add("User-Agent", "Pilgrims.MediaFilesConverter");
                
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get FFmpeg latest version: {StatusCode}", response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                
                // Parse GitHub release JSON to get version
                var version = ExtractVersionFromGitHubResponse(content);
                
                _logger.LogInformation("FFmpeg latest version: {Version}", version);
                return version;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting FFmpeg latest version");
                return null;
            }
        }

        /// <summary>
        /// Extracts version from FFmpeg command output
        /// </summary>
        protected override string? ExtractVersionFromOutput(string output)
        {
            // FFmpeg version output format: "ffmpeg version 4.4.0 Copyright (c) 2000-2021 the FFmpeg developers"
            var pattern = @"ffmpeg\s+version\s+(\d+\.\d+(?:\.\d+)?(?:\.\d+)?(?:-\w+)?)";
            var match = System.Text.RegularExpressions.Regex.Match(output, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : base.ExtractVersionFromOutput(output);
        }

        private string? ExtractVersionFromGitHubResponse(string jsonResponse)
        {
            try
            {
                // Simple JSON parsing for version tag
                var tagPattern = @"""tag_name""\s*:\s*""([^""]+)""";
                var match = System.Text.RegularExpressions.Regex.Match(jsonResponse, tagPattern);
                
                if (match.Success)
                {
                    var tag = match.Groups[1].Value;
                    // Remove 'n' prefix from FFmpeg tags (e.g., "n4.4.0" -> "4.4.0")
                    return tag.StartsWith("n") ? tag.Substring(1) : tag;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing FFmpeg version from GitHub response");
                return null;
            }
        }
    }
}