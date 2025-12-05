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
    /// yt-dlp upgrade service implementation
    /// </summary>
    public class YtDlpUpgradeService : IUtilityUpgradeService
    {
        private readonly IVersionChecker _versionChecker;
        private readonly IDownloadService _downloadService;
        private readonly IExtractionService _extractionService;
        private readonly IUtilityConfigurationService _configurationService;
        private readonly ILogger<YtDlpUpgradeService> _logger;
        private readonly HttpClient _httpClient;

        public YtDlpUpgradeService(
            IVersionChecker versionChecker,
            IDownloadService downloadService,
            IExtractionService extractionService,
            IUtilityConfigurationService configurationService,
            ILogger<YtDlpUpgradeService> logger,
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
        public string UtilityName => "yt-dlp";

        /// <summary>
        /// Gets information about the current state of yt-dlp
        /// </summary>
        public async Task<UtilityInfo> GetUtilityInfoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var executablePath = await _configurationService.ResolveUtilityPathAsync("yt-dlp");
                var currentVersion = await _versionChecker.GetCurrentVersionAsync(executablePath) ?? string.Empty;
                var latestVersion = await _versionChecker.GetLatestVersionAsync() ?? string.Empty;
                
                var versionComparison = _versionChecker.CompareVersions(currentVersion, latestVersion);
                
                return new UtilityInfo
                {
                    Name = "yt-dlp",
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
                _logger.LogError(ex, "Error getting yt-dlp utility information");
                return new UtilityInfo
                {
                    Name = "yt-dlp",
                    IsAvailable = false,
                    StatusMessage = $"Error: {ex.Message}",
                    SuggestedAction = "Check configuration and try again"
                };
            }
        }

        /// <summary>
        /// Checks if an update is available for yt-dlp
        /// </summary>
        public async Task<UpdateCheckResult> IsUpdateAvailableAsync()
        {
            try
            {
                var executablePath = await _configurationService.ResolveUtilityPathAsync("yt-dlp");
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
                _logger.LogError(ex, "Error checking yt-dlp update availability");
                return new UpdateCheckResult
                {
                    UpdateAvailable = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Performs the upgrade of yt-dlp
        /// </summary>
        public async Task<UpgradeResult> UpgradeAsync(IProgress<UpgradeProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            var upgradeProgress = new UpgradeProgress
            {
                UtilityName = "yt-dlp",
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
                var utilityInfo = await GetUtilityInfoAsync(cancellationToken);
                
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
                        Message = "yt-dlp is already up to date",
                        NewVersion = updateCheck.LatestVersion
                    };
                }

                // Download yt-dlp
                upgradeProgress.CurrentOperation = "Downloading yt-dlp";
                upgradeProgress.Percentage = 10;
                progress?.Report(upgradeProgress);

                var downloadUrl = await GetDownloadUrlAsync(cancellationToken);
                if (string.IsNullOrEmpty(downloadUrl))
                {
                    throw new InvalidOperationException("Download URL not available");
                }

                var tempDir = _configurationService.GetTempDirectory();
                var downloadPath = Path.Combine(tempDir, $"yt-dlp-{updateCheck.LatestVersion}.exe");

                var downloadProgress = new Progress<DownloadProgress>(dp =>
                {
                    upgradeProgress.Percentage = 10 + (int)(dp.Percentage * 0.7); // 10% to 80%
                    upgradeProgress.CurrentOperation = $"Downloading yt-dlp ({dp.Percentage:F1}%)";
                    progress?.Report(upgradeProgress);
                });

                var downloadSuccess = await _downloadService.DownloadFileAsync(downloadUrl, downloadPath, downloadProgress, cancellationToken);
                
                if (!downloadSuccess)
                {
                    throw new InvalidOperationException("Failed to download yt-dlp");
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

                // Install yt-dlp
                upgradeProgress.CurrentOperation = "Installing yt-dlp";
                upgradeProgress.Percentage = 85;
                progress?.Report(upgradeProgress);

                var installSuccess = await InstallYtDlpAsync(downloadPath, cancellationToken);
                
                if (!installSuccess)
                {
                    throw new InvalidOperationException("Failed to install yt-dlp");
                }

                // Clean up
                upgradeProgress.CurrentOperation = "Cleaning up";
                progress?.Report(upgradeProgress);

                try
                {
                    File.Delete(downloadPath);
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
                    Message = "yt-dlp upgraded successfully",
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
                _logger.LogError(ex, "Error upgrading yt-dlp");
                
                upgradeProgress.Status = Models.UpgradeStatus.Failed;
                upgradeProgress.CurrentOperation = $"Upgrade failed: {ex.Message}";
                upgradeProgress.ErrorMessage = ex.Message;
                upgradeProgress.EndTime = DateTime.UtcNow;
                progress?.Report(upgradeProgress);

                return new UpgradeResult
                {
                    Success = false,
                    Message = "yt-dlp upgrade failed",
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<string> GetDownloadUrlAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Get the latest release information from GitHub
                var latestReleaseUrl = "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";
                
                using var request = new HttpRequestMessage(HttpMethod.Get, latestReleaseUrl);
                request.Headers.Add("User-Agent", "Pilgrims.MediaFilesConverter");
                
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                
                // Parse JSON to find the Windows executable download URL
                // For simplicity, we'll construct the URL based on the expected pattern
                // In a real implementation, you would parse the JSON properly
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_macos";
                }
                else
                {
                    return "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining yt-dlp download URL");
                
                // Fallback to direct download URL
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_macos";
                }
                else
                {
                    return "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux";
                }
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
                _logger.LogWarning(ex, "Error getting yt-dlp download size");
                return null;
            }
        }

        private async Task<bool> InstallYtDlpAsync(string downloadPath, CancellationToken cancellationToken)
        {
            try
            {
                // Determine installation directory
                var installDir = Path.GetDirectoryName(await _configurationService.ResolveUtilityPathAsync("yt-dlp")) ?? 
                                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                                           "PilgrimsMediaFilesConverter", "Utilities", "yt-dlp");

                if (!Directory.Exists(installDir))
                {
                    Directory.CreateDirectory(installDir);
                }

                // Determine executable name based on platform
                var executableName = _configurationService.GetExecutableName("yt-dlp");
                var installPath = Path.Combine(installDir, executableName);

                // Copy the downloaded file to the installation directory
                File.Copy(downloadPath, installPath, overwrite: true);

                // On Unix-like systems, make the file executable
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        var chmodProcess = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "chmod",
                                Arguments = $"+x \"{installPath}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        chmodProcess.Start();
                        chmodProcess.WaitForExit();
                    }
                    catch (Exception chmodEx)
                    {
                        _logger.LogWarning(chmodEx, "Failed to set executable permissions for yt-dlp");
                    }
                }

                // Update configuration with new path
                await _configurationService.SetUtilityPathAsync("yt-dlp", installPath);

                _logger.LogInformation("yt-dlp installed successfully to: {InstallPath}", installPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error installing yt-dlp");
                return false;
            }
        }

        private string GetStatusMessage(VersionComparisonResult comparison, string currentVersion, string latestVersion)
        {
            return comparison switch
            {
                VersionComparisonResult.UpToDate => "yt-dlp is up to date",
                VersionComparisonResult.UpdateAvailable => $"Update available: {currentVersion} → {latestVersion}",
                VersionComparisonResult.NewerThanLatest => "yt-dlp version is newer than latest release",
                VersionComparisonResult.ComparisonFailed => "Unable to compare versions",
                _ => "Unknown status"
            };
        }

        private string GetSuggestedAction(VersionComparisonResult comparison)
        {
            return comparison switch
            {
                VersionComparisonResult.UpdateAvailable => "Click to upgrade yt-dlp",
                VersionComparisonResult.UpToDate => "No action needed",
                VersionComparisonResult.NewerThanLatest => "No action needed",
                VersionComparisonResult.ComparisonFailed => "Check configuration",
                _ => "Check status"
            };
        }
    }
}