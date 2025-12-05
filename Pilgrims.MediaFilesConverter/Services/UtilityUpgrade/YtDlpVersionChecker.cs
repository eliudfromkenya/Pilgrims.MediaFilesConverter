using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Pilgrims.MediaFilesConverter.Services.UtilityUpgrade
{
    /// <summary>
    /// yt-dlp version checker implementation
    /// </summary>
    public class YtDlpVersionChecker : BaseVersionChecker
    {
        private readonly HttpClient _httpClient;
        private const string YtDlpReleaseUrl = "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";

        public YtDlpVersionChecker(HttpClient httpClient, ILogger<YtDlpVersionChecker> logger) 
            : base(logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Gets the latest available version from yt-dlp GitHub releases
        /// </summary>
        public override async Task<string?> GetLatestVersionAsync()
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, YtDlpReleaseUrl);
                request.Headers.Add("User-Agent", "Pilgrims.MediaFilesConverter");
                
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get yt-dlp latest version: {StatusCode}", response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                
                // Parse GitHub release JSON to get version
                var version = ExtractVersionFromGitHubResponse(content);
                
                _logger.LogInformation("yt-dlp latest version: {Version}", version);
                return version;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting yt-dlp latest version");
                return null;
            }
        }

        /// <summary>
        /// Extracts version from yt-dlp command output
        /// </summary>
        protected override string? ExtractVersionFromOutput(string output)
        {
            // yt-dlp version output format: "yt-dlp version 2021.12.27"
            var pattern = @"yt-dlp\s+version\s+(\d{4}\.\d{2}\.\d{2}(?:\.\d+)?)";
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
                
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing yt-dlp version from GitHub response");
                return null;
            }
        }
    }
}