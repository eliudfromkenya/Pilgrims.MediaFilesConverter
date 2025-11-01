using Avalonia.Controls;
using Avalonia.Data.Converters;
using Pilgrims.MediaFilesConverter.Models;
using System;
using System.Globalization;

namespace Pilgrims.MediaFilesConverter.Converters
{
    public class AudioQualityConverter : IValueConverter
    {
        public ComboBox? ComboBox { get; set; } = null;
        public static AudioQuality AudioQuality { get; private set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is ComboBox cboAudioComboBox)
            {
                ComboBox = cboAudioComboBox;
            }
            return GetText(value);
        }

        private string? GetText(object? value)
        {
            return value switch
            {
                AudioQuality.Low => "Low",
                AudioQuality.Medium => "Medium",
                AudioQuality.High => "High",
                AudioQuality.VeryHigh => "Very High",
                _ => "High"
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ComboBoxItem item)
            {
                string content = item?.Content?.ToString() ?? "High";

                AudioQuality = content switch
                {
                    "Low" => AudioQuality.Low,
                    "Medium" => AudioQuality.Medium,
                    "High" => AudioQuality.High,
                    "Very High" => AudioQuality.VeryHigh,
                    _ => AudioQuality.High
                };
            }

            throw new Exception();
        }
    }
}