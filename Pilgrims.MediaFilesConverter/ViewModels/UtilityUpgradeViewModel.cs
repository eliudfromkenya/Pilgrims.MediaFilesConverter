using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Pilgrims.MediaFilesConverter.Models;
using Pilgrims.MediaFilesConverter.Services;
using Pilgrims.MediaFilesConverter.Services.Interfaces;
using ReactiveUI;

namespace Pilgrims.MediaFilesConverter.ViewModels
{
    /// <summary>
    /// View model for the utility upgrade modal dialog
    /// </summary>
    public class UtilityUpgradeViewModel : ReactiveObject
    {
        private readonly UtilityUpgradeService _upgradeService;
        private readonly ILogger<UtilityUpgradeViewModel> _logger;
        private readonly IMessageService _messageService;

        // FFmpeg properties
        private string _ffmpegStatus = "Not checked";
        private string _ffmpegVersion = "Unknown";
        private string _ffmpegPath = "Not configured";
        private double _ffmpegProgress;
        private bool _ffmpegIsBusy;
        private bool _ffmpegUpdateAvailable;

        // yt-dlp properties
        private string _ytDlpStatus = "Not checked";
        private string _ytDlpVersion = "Unknown";
        private string _ytDlpPath = "Not configured";
        private double _ytDlpProgress;
        private bool _ytDlpIsBusy;
        private bool _ytDlpUpdateAvailable;

        // Commands
        public ICommand CheckAllUpdatesCommand { get; }
        public ICommand CheckFfmpegUpdateCommand { get; }
        public ICommand CheckYtDlpUpdateCommand { get; }
        public ICommand UpgradeFfmpegCommand { get; }
        public ICommand UpgradeYtDlpCommand { get; }

        public UtilityUpgradeViewModel(UtilityUpgradeService upgradeService, ILogger<UtilityUpgradeViewModel> logger, IMessageService messageService)
        {
            _upgradeService = upgradeService ?? throw new ArgumentNullException(nameof(upgradeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            // Initialize commands
            CheckAllUpdatesCommand = ReactiveCommand.CreateFromTask(CheckAllUpdatesAsync);
            CheckFfmpegUpdateCommand = ReactiveCommand.CreateFromTask(CheckFfmpegUpdateAsync);
            CheckYtDlpUpdateCommand = ReactiveCommand.CreateFromTask(CheckYtDlpUpdateAsync);
            UpgradeFfmpegCommand = ReactiveCommand.CreateFromTask(UpgradeFfmpegAsync);
            UpgradeYtDlpCommand = ReactiveCommand.CreateFromTask(UpgradeYtDlpAsync);
        }

        #region Properties

        public string FFmpegStatus
        {
            get => _ffmpegStatus;
            set => this.RaiseAndSetIfChanged(ref _ffmpegStatus, value);
        }

        public string FFmpegVersion
        {
            get => _ffmpegVersion;
            set => this.RaiseAndSetIfChanged(ref _ffmpegVersion, value);
        }

        public string FFmpegPath
        {
            get => _ffmpegPath;
            set => this.RaiseAndSetIfChanged(ref _ffmpegPath, value);
        }

        public double FFmpegProgress
        {
            get => _ffmpegProgress;
            set => this.RaiseAndSetIfChanged(ref _ffmpegProgress, value);
        }

        public bool FFmpegIsBusy
        {
            get => _ffmpegIsBusy;
            set => this.RaiseAndSetIfChanged(ref _ffmpegIsBusy, value);
        }

        public bool FFmpegUpdateAvailable
        {
            get => _ffmpegUpdateAvailable;
            set => this.RaiseAndSetIfChanged(ref _ffmpegUpdateAvailable, value);
        }

        public string YtDlpStatus
        {
            get => _ytDlpStatus;
            set => this.RaiseAndSetIfChanged(ref _ytDlpStatus, value);
        }

        public string YtDlpVersion
        {
            get => _ytDlpVersion;
            set => this.RaiseAndSetIfChanged(ref _ytDlpVersion, value);
        }

        public string YtDlpPath
        {
            get => _ytDlpPath;
            set => this.RaiseAndSetIfChanged(ref _ytDlpPath, value);
        }

        public double YtDlpProgress
        {
            get => _ytDlpProgress;
            set => this.RaiseAndSetIfChanged(ref _ytDlpProgress, value);
        }

        public bool YtDlpIsBusy
        {
            get => _ytDlpIsBusy;
            set => this.RaiseAndSetIfChanged(ref _ytDlpIsBusy, value);
        }

        public bool YtDlpUpdateAvailable
        {
            get => _ytDlpUpdateAvailable;
            set => this.RaiseAndSetIfChanged(ref _ytDlpUpdateAvailable, value);
        }

        #endregion

        /// <summary>
        /// Initialize the view model by loading current utility information
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                await LoadUtilityInfoAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing utility upgrade view model");
                await _messageService.ShowErrorAsync($"Error initializing: {ex.Message}", "Initialization Error");
            }
        }

        /// <summary>
        /// Load current utility information
        /// </summary>
        private async Task LoadUtilityInfoAsync()
        {
            try
            {
                // Load FFmpeg info
                var ffmpegInfo = await _upgradeService.GetFfmpegInfoAsync();
                UpdateFfmpegUI(ffmpegInfo);

                // Load yt-dlp info
                var ytDlpInfo = await _upgradeService.GetYtDlpInfoAsync();
                UpdateYtDlpUI(ytDlpInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading utility information");
                throw;
            }
        }

        /// <summary>
        /// Check for updates for all utilities
        /// </summary>
        private async Task CheckAllUpdatesAsync()
        {
            try
            {
                await CheckFfmpegUpdateAsync();
                await CheckYtDlpUpdateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking all updates");
                await _messageService.ShowErrorAsync($"Error checking updates: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Check for FFmpeg updates
        /// </summary>
        private async Task CheckFfmpegUpdateAsync()
        {
            try
            {
                FFmpegIsBusy = true;
                FFmpegStatus = "Checking for updates...";

                var updateResult = await _upgradeService.CheckFfmpegUpdateAsync();
                FFmpegUpdateAvailable = updateResult.UpdateAvailable;
                
                if (updateResult.UpdateAvailable)
                {
                    FFmpegStatus = $"Update available: {updateResult.CurrentVersion} → {updateResult.LatestVersion}";
                }
                else
                {
                    FFmpegStatus = "Up to date";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking FFmpeg updates");
                FFmpegStatus = $"Error: {ex.Message}";
            }
            finally
            {
                FFmpegIsBusy = false;
            }
        }

        /// <summary>
        /// Check for yt-dlp updates
        /// </summary>
        private async Task CheckYtDlpUpdateAsync()
        {
            try
            {
                YtDlpIsBusy = true;
                YtDlpStatus = "Checking for updates...";

                var updateResult = await _upgradeService.CheckYtDlpUpdateAsync();
                YtDlpUpdateAvailable = updateResult.UpdateAvailable;
                
                if (updateResult.UpdateAvailable)
                {
                    YtDlpStatus = $"Update available: {updateResult.CurrentVersion} → {updateResult.LatestVersion}";
                }
                else
                {
                    YtDlpStatus = "Up to date";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking yt-dlp updates");
                YtDlpStatus = $"Error: {ex.Message}";
            }
            finally
            {
                YtDlpIsBusy = false;
            }
        }

        /// <summary>
        /// Upgrade FFmpeg
        /// </summary>
        private async Task UpgradeFfmpegAsync()
        {
            try
            {
                FFmpegIsBusy = true;
                FFmpegStatus = "Starting upgrade...";
                FFmpegProgress = 0;

                var progress = new Progress<UpgradeProgress>(progress =>
                {
                    FFmpegStatus = progress.CurrentOperation;
                    FFmpegProgress = progress.Percentage;
                });

                var result = await _upgradeService.UpgradeFfmpegAsync(progress);
                
                if (result.Success)
                {
                    FFmpegStatus = "Upgrade completed successfully";
                    FFmpegUpdateAvailable = false;
                    
                    // Reload utility info after upgrade
                    var info = await _upgradeService.GetFfmpegInfoAsync();
                    UpdateFfmpegUI(info);
                }
                else
                {
                    FFmpegStatus = $"Upgrade failed: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upgrading FFmpeg");
                FFmpegStatus = $"Error: {ex.Message}";
            }
            finally
            {
                FFmpegIsBusy = false;
                FFmpegProgress = 0;
            }
        }

        /// <summary>
        /// Upgrade yt-dlp
        /// </summary>
        private async Task UpgradeYtDlpAsync()
        {
            try
            {
                YtDlpIsBusy = true;
                YtDlpStatus = "Starting upgrade...";
                YtDlpProgress = 0;

                var progress = new Progress<UpgradeProgress>(progress =>
                {
                    YtDlpStatus = progress.CurrentOperation;
                    YtDlpProgress = progress.Percentage;
                });

                var result = await _upgradeService.UpgradeYtDlpAsync(progress);
                
                if (result.Success)
                {
                    YtDlpStatus = "Upgrade completed successfully";
                    YtDlpUpdateAvailable = false;
                    
                    // Reload utility info after upgrade
                    var info = await _upgradeService.GetYtDlpInfoAsync();
                    UpdateYtDlpUI(info);
                }
                else
                {
                    YtDlpStatus = $"Upgrade failed: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upgrading yt-dlp");
                YtDlpStatus = $"Error: {ex.Message}";
            }
            finally
            {
                YtDlpIsBusy = false;
                YtDlpProgress = 0;
            }
        }

        /// <summary>
        /// Update FFmpeg UI with utility information
        /// </summary>
        public void UpdateFfmpegUI(UtilityInfo info)
        {
            FFmpegVersion = info.CurrentVersion ?? "Unknown";
                FFmpegPath = info.ExecutablePath ?? "Not configured";
            FFmpegStatus = info.IsAvailable ? "Available" : "Not available";
        }

        /// <summary>
        /// Update yt-dlp UI with utility information
        /// </summary>
        public void UpdateYtDlpUI(UtilityInfo info)
        {
            YtDlpVersion = info.CurrentVersion ?? "Unknown";
            YtDlpPath = info.ExecutablePath ?? "Not configured";
            YtDlpStatus = info.IsAvailable ? "Available" : "Not available";
        }
    }
}