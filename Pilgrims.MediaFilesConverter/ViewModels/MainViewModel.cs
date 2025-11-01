using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Pilgrims.MediaFilesConverter.Converters;
using Pilgrims.MediaFilesConverter.Models;
using Pilgrims.MediaFilesConverter.Services;
using Pilgrims.MediaFilesConverter.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pilgrims.MediaFilesConverter.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly MediaConverterService _converterService;
    private readonly ThemeService _themeService;
    private readonly YouTubeDownloadService _youtubeService;
    private ObservableCollection<MediaFile> _mediaFiles;
    private ConversionSettings _conversionSettings;
    private bool _isProcessing;
    private double _overallProgress;
    private string _statusMessage = "Ready";
    private MediaFile? _selectedMediaFile;
    private YouTubeDownloadViewModel _youtubeDownloadViewModel;
    

    // Output formats for conversion
    public List<string> OutputFormats { get; } = new List<string>
    {
        "mp4", "avi", "mov", "mkv", "wmv", "flv", "webm", "m4v",
        "mp3", "wav", "flac", "aac", "ogg", "wma", "m4a"
    };

    public MainViewModel()
    {
        _converterService = new MediaConverterService();
        _themeService = ThemeService.Instance;
        _youtubeService = YouTubeDownloadService.Instance;
        _mediaFiles = new ObservableCollection<MediaFile>();
        _conversionSettings = new ConversionSettings
        {
            OutputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            OutputFormat = "mp4",
            VideoQuality = VideoQuality.High,
            AudioQuality = AudioQuality.High
        };

        _youtubeDownloadViewModel = new YouTubeDownloadViewModel();
        _youtubeDownloadViewModel.FileDownloaded += OnFileDownloaded;

        // Initialize YouTube format and quality options - REMOVED (now in YouTubeDownloadViewModel)
        // Initialize download type options - REMOVED (now in YouTubeDownloadViewModel)
        // Initialize with video formats by default - REMOVED (now in YouTubeDownloadViewModel)

        // Commands
        AddFilesCommand = ReactiveCommand.CreateFromTask(AddFilesAsync);
        RemoveFileCommand = ReactiveCommand.Create<MediaFile>(RemoveFile);
        ClearAllCommand = ReactiveCommand.Create(ClearAll);
        StartConversionCommand = ReactiveCommand.CreateFromTask(StartConversionAsync, 
            this.WhenAnyValue(x => x.MediaFiles.Count, x => x.IsProcessing, 
                (count, processing) => count > 0 && !processing));
        SelectOutputDirectoryCommand = ReactiveCommand.CreateFromTask(SelectOutputDirectoryAsync);
        SplitFileCommand = ReactiveCommand.CreateFromTask<MediaFile>(SplitFileAsync);
        JoinFilesCommand = ReactiveCommand.CreateFromTask(JoinFilesAsync);
        ToggleThemeCommand = ReactiveCommand.Create(ToggleTheme);
        CloseCommand = ReactiveCommand.Create(() =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
            System.Environment.Exit(0);
        }); 

        // YouTube Commands - REMOVED (now in YouTubeDownloadViewModel)
        OpenYouTubeModalCommand = ReactiveCommand.Create(OpenYouTubeModal);
    }

    public ObservableCollection<MediaFile> MediaFiles
    {
        get => _mediaFiles;
        set => this.RaiseAndSetIfChanged(ref _mediaFiles, value);
    }

    public ConversionSettings ConversionSettings
    {
        get => _conversionSettings;
        set => this.RaiseAndSetIfChanged(ref _conversionSettings, value);
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        set => this.RaiseAndSetIfChanged(ref _isProcessing, value);
    }

    public double OverallProgress
    {
        get => _overallProgress;
        set => this.RaiseAndSetIfChanged(ref _overallProgress, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public MediaFile? SelectedMediaFile
    {
        get => _selectedMediaFile;
        set => this.RaiseAndSetIfChanged(ref _selectedMediaFile, value);
    }

    public YouTubeDownloadViewModel YouTubeDownloadViewModel => _youtubeDownloadViewModel;

    // Commands
    public ReactiveCommand<Unit, Unit> AddFilesCommand { get; }
    public ReactiveCommand<MediaFile, Unit> RemoveFileCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearAllCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    public ReactiveCommand<Unit, Unit> StartConversionCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectOutputDirectoryCommand { get; }
    public ReactiveCommand<MediaFile, Unit> SplitFileCommand { get; }
    public ReactiveCommand<Unit, Unit> JoinFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleThemeCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenYouTubeModalCommand { get; }

    public ThemeService ThemeService => _themeService;

    private async Task AddFilesAsync()
    {
        // This will be implemented with file picker in the view
        await Task.CompletedTask;
    }

    public void AddFiles(string[] filePaths)
    {
        foreach (var filePath in filePaths)
        {
            if (File.Exists(filePath) && !MediaFiles.Any(f => f.FilePath == filePath))
            {
                var mediaFile = new MediaFile(filePath);
                MediaFiles.Add(mediaFile);
            }
        }
        
        StatusMessage = $"{MediaFiles.Count} files ready for conversion";
    }

    private void RemoveFile(MediaFile mediaFile)
    {
        MediaFiles.Remove(mediaFile);
        StatusMessage = $"{MediaFiles.Count} files ready for conversion";
    }

    private void ClearAll()
    {
        MediaFiles.Clear();
        StatusMessage = "Ready";
        OverallProgress = 0;
    }

    private async Task StartConversionAsync()
    {
        if (MediaFiles.Count == 0) return;

        IsProcessing = true;
        StatusMessage = "Converting files...";
        
        var completedFiles = 0;
        var totalFiles = MediaFiles.Count;

        ConversionSettings.AudioQuality = AudioQualityConverter.AudioQuality;
        ConversionSettings.VideoQuality = VideoQualityConverter.VideoQuality;

        foreach (var mediaFile in MediaFiles.ToList())
        {
            if (mediaFile.Status == ConversionStatus.Completed) continue;

            var statusMessage = "Converting first file: ";

            var progress = new Progress<double>(percent =>
            {
                mediaFile.Progress = percent;
                //OverallProgress = (completedFiles * 100 + percent) / totalFiles;
               StatusMessage = $"{statusMessage} => {(percent/100):P}";
            });

            var success = await _converterService.ConvertMediaAsync(mediaFile, ConversionSettings, progress);
            
            if (success)
            {
                completedFiles++;
                statusMessage = $"Converted {completedFiles}/{totalFiles} files.    On file {completedFiles + 1}";
            }
            else
            {
                statusMessage = $"Failed to convert {mediaFile.FileName}";
            }
        }

        IsProcessing = false;
        StatusMessage = completedFiles == totalFiles ? "All conversions completed!" : "Some conversions failed";
        OverallProgress = 100;
    }

    private async Task SelectOutputDirectoryAsync()
    {
        // This will be implemented with folder picker in the view
        await Task.CompletedTask;
    }

    private async Task SplitFileAsync(MediaFile mediaFile)
    {
        // Implementation for file splitting
        await Task.CompletedTask;
    }

    private async Task JoinFilesAsync()
    {
        // Implementation for file joining
        await Task.CompletedTask;
    }

    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
    }

    private void OpenYouTubeModal()
    {
        System.Diagnostics.Debug.WriteLine("OpenYouTubeModal called");
        System.Diagnostics.Debug.WriteLine($"YouTubeDownloadViewModel IsModalOpen before: {_youtubeDownloadViewModel.IsModalOpen}");
        _youtubeDownloadViewModel.OpenModalCommand.Execute(System.Reactive.Unit.Default);
        System.Diagnostics.Debug.WriteLine($"YouTubeDownloadViewModel IsModalOpen after: {_youtubeDownloadViewModel.IsModalOpen}");
    }

    private void OnFileDownloaded(MediaFile downloadedFile)
    {
        MediaFiles.Add(downloadedFile);
    }
}
