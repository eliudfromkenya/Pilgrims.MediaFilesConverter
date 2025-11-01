using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using Pilgrims.MediaFilesConverter.Models;
using Pilgrims.MediaFilesConverter.Services;

namespace Pilgrims.MediaFilesConverter.ViewModels
{
    public class YouTubeDownloadViewModel : ViewModelBase
    {
        private readonly YouTubeDownloadService _youtubeService;
        private string _youtubeUrl = string.Empty;
        private string _videoInfo = string.Empty;
        private bool _isProcessing;
        private double _downloadProgress;
        private string _statusMessage = "Ready";
        private string _selectedDownloadType = "Video";
        private string _selectedFormat = "mp4";
        private string _selectedQuality = "720p";
        private List<string> _downloadTypes;
        private List<string> _availableFormats = new();
        private List<string> _availableQualities = new();
        private string _formatLabel = "Format:";
        private string _qualityLabel = "Quality:";
        private bool _isModalOpen;

        public YouTubeDownloadViewModel()
        {
            _youtubeService = YouTubeDownloadService.Instance;
                       
            // Initialize download type options
            _downloadTypes = new List<string> { "Video", "Audio Only" };
            
            // Initialize with video formats by default
            UpdateFormatsAndQualities();

            // Commands
            GetVideoInfoCommand = ReactiveCommand.CreateFromTask(GetVideoInfoAsync,
                this.WhenAnyValue(x => x.YouTubeUrl, url => !string.IsNullOrWhiteSpace(url) && _youtubeService.IsValidYouTubeUrl(url)));
            
            DownloadVideoCommand = ReactiveCommand.CreateFromTask(DownloadVideoAsync,
                this.WhenAnyValue(x => x.YouTubeUrl, x => x.IsProcessing, 
                    (url, processing) => !string.IsNullOrWhiteSpace(url) && _youtubeService.IsValidYouTubeUrl(url) && !processing));
            
           // OpenModalCommand = ReactiveCommand.Create(OpenModal);
            CloseModalCommand = ReactiveCommand.Create(CloseModal);
            IsModalOpen = true;
        }

        // Properties
        public string YouTubeUrl
        {
            get => _youtubeUrl;
            set => this.RaiseAndSetIfChanged(ref _youtubeUrl, value);
        }

        public string VideoInfo
        {
            get => _videoInfo;
            set => this.RaiseAndSetIfChanged(ref _videoInfo, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => this.RaiseAndSetIfChanged(ref _isProcessing, value);
        }

        public double DownloadProgress
        {
            get => _downloadProgress;
            set => this.RaiseAndSetIfChanged(ref _downloadProgress, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        public string SelectedDownloadType
        {
            get => _selectedDownloadType;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedDownloadType, value);
                UpdateFormatsAndQualities();
            }
        }

        public List<string> DownloadTypes
        {
            get => _downloadTypes;
            set => this.RaiseAndSetIfChanged(ref _downloadTypes, value);
        }

        public string SelectedFormat
        {
            get => _selectedFormat;
            set => this.RaiseAndSetIfChanged(ref _selectedFormat, value);
        }

        public string SelectedQuality
        {
            get => _selectedQuality;
            set => this.RaiseAndSetIfChanged(ref _selectedQuality, value);
        }

        public List<string> AvailableFormats
        {
            get => _availableFormats;
            set => this.RaiseAndSetIfChanged(ref _availableFormats, value);
        }

        public List<string> AvailableQualities
        {
            get => _availableQualities;
            set => this.RaiseAndSetIfChanged(ref _availableQualities, value);
        }

        public string FormatLabel
        {
            get => _formatLabel;
            set => this.RaiseAndSetIfChanged(ref _formatLabel, value);
        }

        public string QualityLabel
        {
            get => _qualityLabel;
            set => this.RaiseAndSetIfChanged(ref _qualityLabel, value);
        }

        public bool IsModalOpen
        {
            get => _isModalOpen;
            set => this.RaiseAndSetIfChanged(ref _isModalOpen, value);
        }

        // Commands
        public ReactiveCommand<Unit, Unit> GetVideoInfoCommand { get; }
        public ReactiveCommand<Unit, Unit> DownloadVideoCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenModalCommand { get; }
        public ReactiveCommand<Unit, Unit> CloseModalCommand { get; }

        // Events
        public event Action<MediaFile>? FileDownloaded;

        private async Task GetVideoInfoAsync()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "Getting video information...";

                var videoInfo = await _youtubeService.GetVideoInfoAsync(YouTubeUrl);
                VideoInfo = videoInfo ?? "Could not retrieve video information";
                StatusMessage = "Video information retrieved";
            }
            catch (Exception ex)
            {
                VideoInfo = $"Error: {ex.Message}";
                StatusMessage = "Failed to get video information";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task DownloadVideoAsync()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "Checking yt-dlp availability...";
                DownloadProgress = 0;

                // Check if yt-dlp is available
                var isAvailable = await _youtubeService.IsYtDlpAvailableAsync();
                if (!isAvailable)
                {
                    StatusMessage = "yt-dlp is not available. Please check installation.";
                    return;
                }

                StatusMessage = "Starting download...";

                var progress = new Progress<string>(message =>
                {
                    StatusMessage = message;
                    
                    // Try to extract progress percentage from yt-dlp output
                    if (message.Contains("%"))
                    {
                        var percentMatch = System.Text.RegularExpressions.Regex.Match(message, @"(\d+(?:\.\d+)?)%");
                        if (percentMatch.Success && double.TryParse(percentMatch.Groups[1].Value, out var percent))
                        {
                            DownloadProgress = percent;
                        }
                    }
                });

                var downloadedFile = await _youtubeService.DownloadVideoAsync(
                    YouTubeUrl, 
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads",
                    SelectedFormat,
                    SelectedQuality,
                    SelectedDownloadType == "Audio Only",
                    progress);

                if (downloadedFile != null)
                {
                    // Notify that a file was downloaded
                    FileDownloaded?.Invoke(downloadedFile);
                    StatusMessage = $"Successfully downloaded: {downloadedFile.FileName}";
                    DownloadProgress = 100;
                    
                    // Clear the YouTube URL after successful download
                    YouTubeUrl = string.Empty;
                    VideoInfo = string.Empty;
                }
                else
                {
                    StatusMessage = "Failed to download YouTube video";
                    DownloadProgress = 0;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Download error: {ex.Message}";
                DownloadProgress = 0;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void OpenModal()
        {
            System.Diagnostics.Debug.WriteLine("YouTubeDownloadViewModel.OpenModal called");
            System.Diagnostics.Debug.WriteLine($"IsModalOpen before: {IsModalOpen}");
            IsModalOpen = true;
            System.Diagnostics.Debug.WriteLine($"IsModalOpen after: {IsModalOpen}");
        }

        private void CloseModal()
        {
            IsModalOpen = false;
        }

        private void UpdateFormatsAndQualities()
        {
            if (SelectedDownloadType == "Audio Only")
            {
                // Audio formats and bitrates
                AvailableFormats = new List<string> { "mp3", "ogg", "wav", "m4a", "flac" };
                AvailableQualities = new List<string> { "64kbps", "128kbps", "192kbps", "256kbps", "320kbps", "best", "worst" };
                FormatLabel = "Audio Format:";
                QualityLabel = "Bitrate:";
                
                // Set default audio values if current selection is not valid
                if (!AvailableFormats.Contains(SelectedFormat))
                    SelectedFormat = "mp3";
                if (!AvailableQualities.Contains(SelectedQuality))
                    SelectedQuality = "192kbps";
            }
            else
            {
                // Video formats and qualities
                AvailableFormats = new List<string> { "mp4", "webm", "mkv", "avi", "mov" };
                AvailableQualities = new List<string> { "144p", "240p", "360p", "480p", "720p", "1080p", "1440p", "2160p", "best", "worst" };
                FormatLabel = "Video Format:";
                QualityLabel = "Quality:";
                
                // Set default video values if current selection is not valid
                if (!AvailableFormats.Contains(SelectedFormat))
                    SelectedFormat = "mp4";
                if (!AvailableQualities.Contains(SelectedQuality))
                    SelectedQuality = "720p";
            }
        }
    }
}