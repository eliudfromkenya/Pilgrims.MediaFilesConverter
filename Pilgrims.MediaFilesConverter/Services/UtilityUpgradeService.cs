using System;
using System.IO;
using System.Threading.Tasks;
using FFMpegCore;
using Microsoft.Extensions.Logging;
using Pilgrims.MediaFilesConverter.Models;
using Pilgrims.MediaFilesConverter.Services.UtilityUpgrade;

namespace Pilgrims.MediaFilesConverter.Services
{
    /// <summary>
    /// Service for managing utility upgrades (FFmpeg and yt-dlp)
    /// </summary>
    public class UtilityUpgradeService
    {
        private readonly ILogger<UtilityUpgradeService> _logger;
        private readonly FFmpegUpgradeService _ffmpegUpgradeService;
        private readonly YtDlpUpgradeService _ytDlpUpgradeService;

        public UtilityUpgradeService(
            ILogger<UtilityUpgradeService> logger,
            FFmpegUpgradeService ffmpegUpgradeService,
            YtDlpUpgradeService ytDlpUpgradeService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ffmpegUpgradeService = ffmpegUpgradeService ?? throw new ArgumentNullException(nameof(ffmpegUpgradeService));
            _ytDlpUpgradeService = ytDlpUpgradeService ?? throw new ArgumentNullException(nameof(ytDlpUpgradeService));
        }

        public async Task<UtilityInfo> GetFFmpegInfoAsync()
        {
            return await _ffmpegUpgradeService.GetUtilityInfoAsync();
        }

        public async Task<UtilityInfo> GetYtDlpInfoAsync()
        {
            return await _ytDlpUpgradeService.GetUtilityInfoAsync();
        }

        public async Task<UpdateCheckResult> CheckFFmpegForUpdatesAsync()
        {
            return await _ffmpegUpgradeService.IsUpdateAvailableAsync();
        }

        public async Task<UpdateCheckResult> CheckYtDlpForUpdatesAsync()
        {
            return await _ytDlpUpgradeService.IsUpdateAvailableAsync();
        }

        public async Task<UpgradeResult> UpgradeFFmpegAsync(IProgress<UpgradeProgress> progress)
        {
            return await _ffmpegUpgradeService.UpgradeAsync(progress);
        }

        public async Task<UpgradeResult> UpgradeYtDlpAsync(IProgress<UpgradeProgress> progress)
        {
            return await _ytDlpUpgradeService.UpgradeAsync(progress);
        }

        public async Task<bool> IsFFmpegAvailableAsync()
        {
            var info = await GetFFmpegInfoAsync();
            return info.IsAvailable;
        }

        public async Task<bool> IsYtDlpAvailableAsync()
        {
            var info = await GetYtDlpInfoAsync();
            return info.IsAvailable;
        }

        public async Task<string?> GetFFmpegPathAsync()
        {
            var info = await GetFFmpegInfoAsync();
            return info.ExecutablePath;
        }

        public async Task<string?> GetYtDlpPathAsync()
        {
            var info = await GetYtDlpInfoAsync();
            return info.ExecutablePath;
        }

        // Alias methods for ViewModel compatibility
        public async Task<UtilityInfo> GetFfmpegInfoAsync() => await GetFFmpegInfoAsync();
        public async Task<UpdateCheckResult> CheckFfmpegUpdateAsync() => await CheckFFmpegForUpdatesAsync();
        public async Task<UpgradeResult> UpgradeFfmpegAsync(IProgress<UpgradeProgress> progress) => await UpgradeFFmpegAsync(progress);
        public async Task<UpdateCheckResult> CheckYtDlpUpdateAsync() => await CheckYtDlpForUpdatesAsync();

        public async Task<bool> ConfigureFFmpegAsync()
        {
            try
            {
                var ffmpegPath = await GetFFmpegPathAsync();
                if (!string.IsNullOrEmpty(ffmpegPath) && File.Exists(ffmpegPath))
                {
                    var ffmpegDirectory = Path.GetDirectoryName(ffmpegPath);
                    if (!string.IsNullOrEmpty(ffmpegDirectory))
                    {
                        GlobalFFOptions.Configure(new FFOptions { BinaryFolder = ffmpegDirectory });
                        _logger.LogInformation("FFmpeg configured successfully with path: {Path}", ffmpegPath);
                        return true;
                    }
                }

                _logger.LogWarning("FFmpeg path not found or invalid: {Path}", ffmpegPath);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure FFmpeg");
                return false;
            }
        }
    }
}