using System.Collections.Generic;
using ReactiveUI;

namespace Pilgrims.MediaFilesConverter.Models
{
    public class ConversionSettings : ReactiveObject
    {
        private string _outputFormat = string.Empty;
        private string _outputDirectory = string.Empty;
        private VideoQuality _videoQuality = VideoQuality.High;
        private AudioQuality _audioQuality = AudioQuality.High;
        private int _videoBitrate = 2000;
        private int _audioBitrate = 192;
        private string _resolution = string.Empty;
        private double _compressionRatio = 1.0;
        private bool _maintainAspectRatio = true;
        private bool _enableTrimming;
        private int? _startTime;
        private int? _endTime;
        private bool _enableCropping;
        private CropSettings _cropSettings = new();

        public string OutputFormat
        {
            get => _outputFormat;
            set => this.RaiseAndSetIfChanged(ref _outputFormat, value);
        }

        public string OutputDirectory
        {
            get => _outputDirectory;
            set => this.RaiseAndSetIfChanged(ref _outputDirectory, value);
        }

        public VideoQuality VideoQuality
        {
            get => _videoQuality;
            set => this.RaiseAndSetIfChanged(ref _videoQuality, value);
        }

        public AudioQuality AudioQuality
        {
            get => _audioQuality;
            set => this.RaiseAndSetIfChanged(ref _audioQuality, value);
        }

        public int VideoBitrate
        {
            get => _videoBitrate;
            set => this.RaiseAndSetIfChanged(ref _videoBitrate, value);
        }

        public int AudioBitrate
        {
            get => _audioBitrate;
            set => this.RaiseAndSetIfChanged(ref _audioBitrate, value);
        }

        public string Resolution
        {
            get => _resolution;
            set => this.RaiseAndSetIfChanged(ref _resolution, value);
        }

        public double CompressionRatio
        {
            get => _compressionRatio;
            set => this.RaiseAndSetIfChanged(ref _compressionRatio, value);
        }

        public bool MaintainAspectRatio
        {
            get => _maintainAspectRatio;
            set => this.RaiseAndSetIfChanged(ref _maintainAspectRatio, value);
        }

        public bool EnableTrimming
        {
            get => _enableTrimming;
            set => this.RaiseAndSetIfChanged(ref _enableTrimming, value);
        }

        public int? StartTime
        {
            get => _startTime;
            set => this.RaiseAndSetIfChanged(ref _startTime, value);
        }

        public int? EndTime
        {
            get => _endTime;
            set => this.RaiseAndSetIfChanged(ref _endTime, value);
        }

        public bool EnableCropping
        {
            get => _enableCropping;
            set => this.RaiseAndSetIfChanged(ref _enableCropping, value);
        }

        public int CropX
        {
            get => _cropSettings.X;
            set => _cropSettings.X = value;
        }

        public int CropY
        {
            get => _cropSettings.Y;
            set => _cropSettings.Y = value;
        }

        public int CropWidth
        {
            get => _cropSettings.Width;
            set => _cropSettings.Width = value;
        }

        public int CropHeight
        {
            get => _cropSettings.Height;
            set => _cropSettings.Height = value;
        }

        public static List<string> GetSupportedVideoFormats() => new()
        {
            "mp4", "avi", "mov", "mkv", "wmv", "flv", "webm", "m4v"
        };

        public static List<string> GetSupportedAudioFormats() => new()
        {
            "mp3", "wav", "flac", "aac", "ogg", "wma", "m4a"
        };

        public static List<string> GetVideoResolutions() => new()
        {
            "Original", "3840x2160", "1920x1080", "1280x720", "854x480", "640x360"
        };
    }

    public class CropSettings : ReactiveObject
    {
        private int _x;
        private int _y;
        private int _width;
        private int _height;

        public int X
        {
            get => _x;
            set => this.RaiseAndSetIfChanged(ref _x, value);
        }

        public int Y
        {
            get => _y;
            set => this.RaiseAndSetIfChanged(ref _y, value);
        }

        public int Width
        {
            get => _width;
            set => this.RaiseAndSetIfChanged(ref _width, value);
        }

        public int Height
        {
            get => _height;
            set => this.RaiseAndSetIfChanged(ref _height, value);
        }
    }

    public enum VideoQuality
    {
        Low,
        Medium,
        High,
        VeryHigh
    }

    public enum AudioQuality
    {
        Low,
        Medium,
        High,
        VeryHigh
    }
}