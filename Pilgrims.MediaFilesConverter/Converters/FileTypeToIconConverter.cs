using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Pilgrims.MediaFilesConverter.Models;

namespace Pilgrims.MediaFilesConverter.Converters
{
    public class FileTypeToIconConverter : IValueConverter
    {
        public static readonly FileTypeToIconConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is MediaFileType fileType)
            {
                return fileType switch
                {
                    MediaFileType.Video => "🎬",
                    MediaFileType.Audio => "🎵",
                    _ => "📄"
                };
            }
            return "📄";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToPlayPauseConverter : IValueConverter
    {
        public static readonly BoolToPlayPauseConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isPlaying)
            {
                return isPlaying ? "⏸" : "▶";
            }
            return "▶";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ConversionStatusToBoolConverter : IValueConverter
    {
        public static readonly ConversionStatusToBoolConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ConversionStatus status)
            {
                return status == ConversionStatus.Processing;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public static readonly BoolToVisibilityConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToClassConverter : IValueConverter
    {
        public static readonly StatusToClassConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ConversionStatus status)
            {
                return status switch
                {
                    ConversionStatus.Pending => "status-pending",
                    ConversionStatus.Processing => "status-processing",
                    ConversionStatus.Completed => "status-completed",
                    ConversionStatus.Failed => "status-failed",
                    _ => "status-pending"
                };
            }
            return "status-pending";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CountToBoolConverter : IValueConverter
    {
        public static readonly CountToBoolConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count > 1; // Need at least 2 files to join
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}