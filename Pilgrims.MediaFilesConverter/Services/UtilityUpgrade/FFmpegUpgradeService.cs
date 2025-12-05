using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pilgrims.MediaFilesConverter.Models;

namespace Pilgrims.MediaFilesConverter.Services.UtilityUpgrade
{
    /// <summary>
    /// FFmpeg upgrade service implementation
    /// </summary>
    public class FFmpegUpgradeService : IUtilityUpgradeService
    {
        private readonly IVersionChecker _versionChecker;
        private readonly IDownloadService _downloadService;
        private readonly IExtractionService _extractionService;
        private readonly IUtilityConfigurationService _configurationService;
        private readonly ILogger<FFmpegUpgradeService> _logger;
        private readonly HttpClient _httpClient;

        public FFmpegUpgradeService(
            IVersionChecker versionChecker,
            IDownloadService downloadService,
            IExtractionService extractionService,
            IUtilityConfigurationService configurationService,
            ILogger<FFmpegUpgradeService> logger,
            HttpClient httpClient)
        {
            _versionChecker = versionChecker ?? throw new ArgumentNullException(nameof(versionChecker));
            _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
            _extractionService = extractionService ?? throw new ArgumentNullException(nameof(extractionService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Gets the name of the utility
        /// </summary>
        public string UtilityName => "FFmpeg";

        /// <summary>
        /// Gets information about the current state of FFmpeg
        /// </summary>
        public async Task<UtilityInfo> GetUtilityInfoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var executablePath = await _configurationService.ResolveUtilityPathAsync("ffmpeg");
                var currentVersion = await _versionChecker.GetCurrentVersionAsync(executablePath) ?? string.Empty;
                var latestVersion = await _versionChecker.GetLatestVersionAsync() ?? string.Empty;
                
                var versionComparison = _versionChecker.CompareVersions(currentVersion, latestVersion);
                
                return new UtilityInfo
                {
                    Name = "FFmpeg",
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion,
                    ExecutablePath = executablePath,
                    IsAvailable = !string.IsNullOrEmpty(executablePath) && File.Exists(executablePath),
                    StatusMessage = GetStatusMessage(versionComparison, currentVersion, latestVersion),
                    SuggestedAction = GetSuggestedAction(versionComparison),
                    DownloadUrl = await GetDownloadUrlAsync(cancellationToken),
                    DownloadSize = await GetDownloadSizeAsync(cancellationToken)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting FFmpeg utility information");
                return new UtilityInfo
                {
                    Name = "FFmpeg",
                    IsAvailable = false,
                    StatusMessage = $"Error: {ex.Message}",
                    SuggestedAction = "Check configuration and try again"
                };
            }
        }

        /// <summary>
        /// Checks if an update is available for FFmpeg
        /// </summary>
        public async Task<UpdateCheckResult> IsUpdateAvailableAsync()
        {
            try
            {
                var executablePath = await _configurationService.ResolveUtilityPathAsync("ffmpeg");
                var currentVersion = await _versionChecker.GetCurrentVersionAsync(executablePath) ?? string.Empty;
                var latestVersion = await _versionChecker.GetLatestVersionAsync() ?? string.Empty;
                var comparison = _versionChecker.CompareVersions(currentVersion, latestVersion);
                
                return new UpdateCheckResult
                {
                    UpdateAvailable = comparison == VersionComparisonResult.UpdateAvailable,
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking FFmpeg update availability");
                return new UpdateCheckResult
                {
                    UpdateAvailable = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Performs the upgrade of FFmpeg
        /// </summary>
        public async Task<UpgradeResult> UpgradeAsync(IProgress<UpgradeProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            var upgradeProgress = new UpgradeProgress
            {
                UtilityName = "FFmpeg",
                Status = Models.UpgradeStatus.Started,
                Percentage = 0,
                CurrentOperation = "Initializing upgrade",
                StartTime = DateTime.UtcNow
            };

            try
            {
                progress?.Report(upgradeProgress);

                // Get utility info
                upgradeProgress.CurrentOperation = "Checking current version";
                progress?.Report(upgradeProgress);

                var updateCheck = await IsUpdateAvailableAsync();
                
                if (!updateCheck.UpdateAvailable)
                {
                    upgradeProgress.Status = Models.UpgradeStatus.Completed;
                    upgradeProgress.Percentage = 100;
                    upgradeProgress.CurrentOperation = "Already up to date";
                    upgradeProgress.EndTime = DateTime.UtcNow;
                    progress?.Report(upgradeProgress);

                    return new UpgradeResult
                    {
                        Success = true,
                        Message = "FFmpeg is already up to date",
                        NewVersion = updateCheck.LatestVersion
                    };
                }

                // Download FFmpeg
                upgradeProgress.CurrentOperation = "Downloading FFmpeg";
                upgradeProgress.Percentage = 10;
                progress?.Report(upgradeProgress);

                var downloadUrl = await GetDownloadUrlAsync(cancellationToken);
                if (string.IsNullOrEmpty(downloadUrl))
                {
                    throw new InvalidOperationException("Download URL not available");
                }

                var tempDir = _configurationService.GetTempDirectory();
                var downloadPath = Path.Combine(tempDir, $"ffmpeg-{updateCheck.LatestVersion}.zip");

                var downloadProgress = new Progress<DownloadProgress>(dp =>
                {
                    upgradeProgress.Percentage = 10 + (int)(dp.Percentage * 0.7); // 10% to 80%
                    upgradeProgress.CurrentOperation = $"Downloading FFmpeg ({dp.Percentage:F1}%)";
                    progress?.Report(upgradeProgress);
                });

                var downloadSuccess = await _downloadService.DownloadFileAsync(downloadUrl, downloadPath, downloadProgress, cancellationToken);
                
                if (!downloadSuccess)
                {
                    throw new InvalidOperationException("Failed to download FFmpeg");
                }

                // Validate download
                upgradeProgress.CurrentOperation = "Validating download";
                upgradeProgress.Percentage = 80;
                progress?.Report(upgradeProgress);

                var isValid = await _downloadService.ValidateDownloadedFileAsync(downloadPath);
                if (!isValid)
                {
                    throw new InvalidOperationException("Download validation failed");
                }

                // Extract FFmpeg
                upgradeProgress.CurrentOperation = "Extracting FFmpeg";
                upgradeProgress.Percentage = 85;
                progress?.Report(upgradeProgress);

                var extractionDir = Path.Combine(tempDir, "ffmpeg-extract");
                var extractionProgress = new Progress<ExtractionProgress>(ep =>
                {
                    upgradeProgress.Percentage = 85 + (int)(ep.Percentage * 0.1); // 85% to 95%
                    upgradeProgress.CurrentOperation = $"Extracting FFmpeg ({ep.Percentage:F1}%)";
                    progress?.Report(upgradeProgress);
                });

                var extractSuccess = await _extractionService.ExtractArchiveAsync(downloadPath, extractionDir, extractionProgress);
                
                if (!extractSuccess)
                {
                    throw new InvalidOperationException("Failed to extract FFmpeg");
                }

                // Find and copy executable files
                upgradeProgress.CurrentOperation = "Installing FFmpeg";
                upgradeProgress.Percentage = 95;
                progress?.Report(upgradeProgress);

                var installSuccess = await InstallFFmpegAsync(extractionDir, cancellationToken);
                
                if (!installSuccess)
                {
                    throw new InvalidOperationException("Failed to install FFmpeg");
                }

                // Clean up
                upgradeProgress.CurrentOperation = "Cleaning up";
                progress?.Report(upgradeProgress);

                try
                {
                    File.Delete(downloadPath);
                    Directory.Delete(extractionDir, recursive: true);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Error cleaning up temporary files");
                }

                // Finalize
                upgradeProgress.Status = Models.UpgradeStatus.Completed;
                upgradeProgress.Percentage = 100;
                upgradeProgress.CurrentOperation = "Upgrade completed successfully";
                upgradeProgress.EndTime = DateTime.UtcNow;
                progress?.Report(upgradeProgress);

                return new UpgradeResult
                {
                    Success = true,
                    Message = "FFmpeg upgraded successfully",
                    NewVersion = updateCheck.LatestVersion
                };
            }
            catch (OperationCanceledException)
            {
                upgradeProgress.Status = Models.UpgradeStatus.Cancelled;
                upgradeProgress.CurrentOperation = "Upgrade cancelled";
                upgradeProgress.EndTime = DateTime.UtcNow;
                progress?.Report(upgradeProgress);

                return new UpgradeResult
                {
                    Success = false,
                    Message = "Upgrade was cancelled",
                    ErrorMessage = "Operation was cancelled"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upgrading FFmpeg");
                
                upgradeProgress.Status = Models.UpgradeStatus.Failed;
                upgradeProgress.CurrentOperation = $"Upgrade failed: {ex.Message}";
                upgradeProgress.ErrorMessage = ex.Message;
                upgradeProgress.EndTime = DateTime.UtcNow;
                progress?.Report(upgradeProgress);

                return new UpgradeResult
                {
                    Success = false,
                    Message = "FFmpeg upgrade failed",
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<string> GetDownloadUrlAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Determine platform-specific download URL
                var baseUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest";
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return $"{baseUrl}/ffmpeg-n6.1-latest-win64-gpl-6.1.zip";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return $"{baseUrl}/ffmpeg-n6.1-latest-macos64-gpl-6.1.zip";
                }
                else
                {
                    return $"{baseUrl}/ffmpeg-n6.1-latest-linux64-gpl-6.1.zip";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining FFmpeg download URL");
                throw;
            }
        }

        private async Task<long?> GetDownloadSizeAsync(CancellationToken cancellationToken)
        {
            try
            {
                var downloadUrl = await GetDownloadUrlAsync(cancellationToken);
                return await _downloadService.GetFileSizeAsync(downloadUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting FFmpeg download size");
                return null;
            }
        }

        private async Task<bool> InstallFFmpegAsync(string extractionDir, CancellationToken cancellationToken)
        {
            try
            {
                // Find FFmpeg executables in the extraction directory
                var ffmpegExe = FindExecutable(extractionDir, "ffmpeg");
                var ffprobeExe = FindExecutable(extractionDir, "ffprobe");
                var ffplayExe = FindExecutable(extractionDir, "ffplay");

                if (string.IsNullOrEmpty(ffmpegExe))
                {
                    _logger.LogError("FFmpeg executable not found in extraction directory: {ExtractionDir}", extractionDir);
                    return false;
                }

                // Determine installation directory
                var installDir = Path.GetDirectoryName(await _configurationService.ResolveUtilityPathAsync("ffmpeg")) ?? 
                                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                                           "PilgrimsMediaFilesConverter", "Utilities", "ffmpeg");

                if (!Directory.Exists(installDir))
                {
                    Directory.CreateDirectory(installDir);
                }

                // Copy executables
                var ffmpegDest = Path.Combine(installDir, Path.GetFileName(ffmpegExe));
                File.Copy(ffmpegExe, ffmpegDest, overwrite: true);

                if (!string.IsNullOrEmpty(ffprobeExe))
                {
                    var ffprobeDest = Path.Combine(installDir, Path.GetFileName(ffprobeExe));
                    File.Copy(ffprobeExe, ffprobeDest, overwrite: true);
                }

                if (!string.IsNullOrEmpty(ffplayExe))
                {
                    var ffplayDest = Path.Combine(installDir, Path.GetFileName(ffplayExe));
                    File.Copy(ffplayExe, ffplayDest, overwrite: true);
                }

                // Update configuration with new path
                await _configurationService.SetUtilityPathAsync("ffmpeg", ffmpegDest);

                _logger.LogInformation("FFmpeg installed successfully to: {InstallDir}", installDir);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error installing FFmpeg");
                return false;
            }
        }

        private string FindExecutable(string directory, string executableName)
        {
            try
            {
                var exeName = _configurationService.GetExecutableName(executableName);
                var fullPath = Path.Combine(directory, exeName);
                
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }

                // Search recursively in subdirectories
                var files = Directory.GetFiles(directory, exeName, SearchOption.AllDirectories);
                return files.Length > 0 ? files[0] : string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error finding executable {ExecutableName} in {Directory}", executableName, directory);
                return string.Empty;
            }
        }

        private string GetStatusMessage(VersionComparisonResult comparison, string currentVersion, string latestVersion)
        {
            return comparison switch
            {
                VersionComparisonResult.UpToDate => "FFmpeg is up to date",
                VersionComparisonResult.UpdateAvailable => $"Update available: {currentVersion} → {latestVersion}",
                VersionComparisonResult.NewerThanLatest => "FFmpeg version is newer than latest release",
                VersionComparisonResult.ComparisonFailed => "Unable to compare versions",
                _ => "Unknown status"
            };
        }

        private string GetSuggestedAction(VersionComparisonResult comparison)
        {
            return comparison switch
            {
                VersionComparisonResult.UpdateAvailable => "Click to upgrade FFmpeg",
                VersionComparisonResult.UpToDate => "No action needed",
                VersionComparisonResult.NewerThanLatest => "No action needed",
                VersionComparisonResult.ComparisonFailed => "Check configuration",
                _ => "Check status"
            };
        }
    }
}