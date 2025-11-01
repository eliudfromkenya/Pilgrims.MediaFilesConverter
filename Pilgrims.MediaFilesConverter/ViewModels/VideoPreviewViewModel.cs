using System;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using Pilgrims.MediaFilesConverter.Models;

namespace Pilgrims.MediaFilesConverter.ViewModels;

public class VideoPreviewViewModel : ViewModelBase
{
    private string? _videoPath;
    private TimeSpan _currentTime;
    private TimeSpan _totalDuration;
    private bool _isPlaying;
    private bool _isCropMode;
    private bool _isJoinSplitMode;
    private double _cropX;
    private double _cropY;
    private double _cropWidth = 200;
    private double _cropHeight = 150;
    private TimeSpan _startTime;
    private TimeSpan _endTime;

    public VideoPreviewViewModel()
    {
        // Commands
        PlayPauseCommand = ReactiveCommand.Create(PlayPause);
        SeekToStartCommand = ReactiveCommand.Create(SeekToStart);
        SeekToEndCommand = ReactiveCommand.Create(SeekToEnd);
        SetStartTimeCommand = ReactiveCommand.Create(SetStartTime);
        SetEndTimeCommand = ReactiveCommand.Create(SetEndTime);
    }

    public string? VideoPath
    {
        get => _videoPath;
        set => this.RaiseAndSetIfChanged(ref _videoPath, value);
    }

    public TimeSpan CurrentTime
    {
        get => _currentTime;
        set => this.RaiseAndSetIfChanged(ref _currentTime, value);
    }

    public TimeSpan TotalDuration
    {
        get => _totalDuration;
        set => this.RaiseAndSetIfChanged(ref _totalDuration, value);
    }

    public double CurrentTimeSeconds
    {
        get => _currentTime.TotalSeconds;
        set
        {
            CurrentTime = TimeSpan.FromSeconds(value);
            this.RaisePropertyChanged();
        }
    }

    public double TotalDurationSeconds
    {
        get => _totalDuration.TotalSeconds;
        set
        {
            TotalDuration = TimeSpan.FromSeconds(value);
            this.RaisePropertyChanged();
        }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
    }

    public bool IsCropMode
    {
        get => _isCropMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _isCropMode, value);
            if (value)
            {
                IsJoinSplitMode = false;
            }
        }
    }

    public bool IsJoinSplitMode
    {
        get => _isJoinSplitMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _isJoinSplitMode, value);
            if (value)
            {
                IsCropMode = false;
            }
        }
    }

    public double CropX
    {
        get => _cropX;
        set => this.RaiseAndSetIfChanged(ref _cropX, value);
    }

    public double CropY
    {
        get => _cropY;
        set => this.RaiseAndSetIfChanged(ref _cropY, value);
    }

    public double CropWidth
    {
        get => _cropWidth;
        set => this.RaiseAndSetIfChanged(ref _cropWidth, value);
    }

    public double CropHeight
    {
        get => _cropHeight;
        set => this.RaiseAndSetIfChanged(ref _cropHeight, value);
    }

    public TimeSpan StartTime
    {
        get => _startTime;
        set => this.RaiseAndSetIfChanged(ref _startTime, value);
    }

    public TimeSpan EndTime
    {
        get => _endTime;
        set => this.RaiseAndSetIfChanged(ref _endTime, value);
    }

    // Commands
    public ReactiveCommand<Unit, Unit> PlayPauseCommand { get; }
    public ReactiveCommand<Unit, Unit> SeekToStartCommand { get; }
    public ReactiveCommand<Unit, Unit> SeekToEndCommand { get; }
    public ReactiveCommand<Unit, Unit> SetStartTimeCommand { get; }
    public ReactiveCommand<Unit, Unit> SetEndTimeCommand { get; }

    public void LoadVideo(string videoPath)
    {
        VideoPath = videoPath;
        // In a real implementation, you would load video metadata here
        // For now, we'll set some dummy values
        TotalDuration = TimeSpan.FromMinutes(5); // 5 minutes as example
        CurrentTime = TimeSpan.Zero;
        IsPlaying = false;
    }

    private void PlayPause()
    {
        IsPlaying = !IsPlaying;
        // In a real implementation, you would control video playback here
    }

    private void SeekToStart()
    {
        CurrentTime = TimeSpan.Zero;
        // In a real implementation, you would seek the video to start
    }

    private void SeekToEnd()
    {
        CurrentTime = TotalDuration;
        // In a real implementation, you would seek the video to end
    }

    private void SetStartTime()
    {
        StartTime = CurrentTime;
    }

    private void SetEndTime()
    {
        EndTime = CurrentTime;
    }

    public CropSettings GetCropSettings()
    {
        return new CropSettings
        {
            X = (int)CropX,
            Y = (int)CropY,
            Width = (int)CropWidth,
            Height = (int)CropHeight
        };
    }

    public (TimeSpan Start, TimeSpan End) GetTimeRange()
    {
        return (StartTime, EndTime);
    }
}

public class CropSettings
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}