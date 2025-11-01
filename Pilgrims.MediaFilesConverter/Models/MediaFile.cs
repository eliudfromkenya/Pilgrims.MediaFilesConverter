using System;
using System.ComponentModel;
using System.IO;
using ReactiveUI;

namespace Pilgrims.MediaFilesConverter.Models
{
    public class MediaFile : ReactiveObject
    {
        private string _filePath = string.Empty;
        private string _fileName = string.Empty;
        private string _fileExtension = string.Empty;
        private long _fileSize;
        private string _duration = string.Empty;
        private string _resolution = string.Empty;
        private MediaFileType _fileType;
        private ConversionStatus _status = ConversionStatus.Pending;
        private double _progress;
        private string _outputPath = string.Empty;

        public string FilePath
        {
            get => _filePath;
            set => this.RaiseAndSetIfChanged(ref _filePath, value);
        }

        public string FileName
        {
            get => _fileName;
            set => this.RaiseAndSetIfChanged(ref _fileName, value);
        }

        public string FileExtension
        {
            get => _fileExtension;
            set => this.RaiseAndSetIfChanged(ref _fileExtension, value);
        }

        public long FileSize
        {
            get => _fileSize;
            set => this.RaiseAndSetIfChanged(ref _fileSize, value);
        }

        public string FileSizeFormatted => FormatFileSize(FileSize);

        public string Duration
        {
            get => _duration;
            set => this.RaiseAndSetIfChanged(ref _duration, value);
        }

        public string Resolution
        {
            get => _resolution;
            set => this.RaiseAndSetIfChanged(ref _resolution, value);
        }

        public MediaFileType FileType
        {
            get => _fileType;
            set => this.RaiseAndSetIfChanged(ref _fileType, value);
        }

        public ConversionStatus Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        public double Progress
        {
            get => _progress;
            set => this.RaiseAndSetIfChanged(ref _progress, value);
        }

        public string OutputPath
        {
            get => _outputPath;
            set => this.RaiseAndSetIfChanged(ref _outputPath, value);
        }

        public MediaFile(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            FileExtension = Path.GetExtension(filePath).ToLower();
            FileSize = new FileInfo(filePath).Length;
            FileType = DetermineFileType(FileExtension);
        }

        private MediaFileType DetermineFileType(string extension)
        {
            return extension switch
            {
                ".mp4" or ".avi" or ".mov" or ".mkv" or ".wmv" or ".flv" or ".webm" or ".m4v" => MediaFileType.Video,
                ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" or ".wma" or ".m4a" => MediaFileType.Audio,
                _ => MediaFileType.Unknown
            };
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    public enum MediaFileType
    {
        Video,
        Audio,
        Unknown
    }

    public enum ConversionStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled
    }
}