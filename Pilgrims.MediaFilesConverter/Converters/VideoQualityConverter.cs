using Avalonia.Controls;
using Avalonia.Data.Converters;
using Pilgrims.MediaFilesConverter.Models;
using System;
using System.Globalization;

namespace Pilgrims.MediaFilesConverter.Converters
{
    public class VideoQualityConverter : IValueConverter
    {
        public ComboBox? ComboBox { get; set; } = null;
        public static VideoQuality VideoQuality { get; private set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is ComboBox cboVideoComboBox)
            {
                ComboBox = cboVideoComboBox;
            }
            return GetText(value);
        }

        private string? GetText(object? value)
        {
            return value switch
            {
                VideoQuality.Low => "Low",
                VideoQuality.Medium => "Medium",
                VideoQuality.High => "High",
                VideoQuality.VeryHigh => "Very High",
                _ => "High"
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {            
            if (value is ComboBoxItem item)
            {
                string content = item?.Content?.ToString() ?? "High";
                
                VideoQuality = content switch
                {
                    "Low" => VideoQuality.Low,
                    "Medium" => VideoQuality.Medium,
                    "High" => VideoQuality.High,
                    "Very High" => VideoQuality.VeryHigh,
                    _ => VideoQuality.High
                };
            }

            throw new Exception();
        }
    }
}