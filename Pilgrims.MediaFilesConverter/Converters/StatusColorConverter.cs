using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Pilgrims.MediaFilesConverter.Converters
{
    /// <summary>
    /// Converts status text to appropriate color
    /// </summary>
    public class StatusColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                if (status.Contains("Error", StringComparison.OrdinalIgnoreCase))
                    return "#DC3545"; // Red
                if (status.Contains("available", StringComparison.OrdinalIgnoreCase) || 
                    status.Contains("Up to date", StringComparison.OrdinalIgnoreCase))
                    return "#28A745"; // Green
                if (status.Contains("Checking", StringComparison.OrdinalIgnoreCase) || 
                    status.Contains("Starting", StringComparison.OrdinalIgnoreCase))
                    return "#FFC107"; // Yellow
                if (status.Contains("completed", StringComparison.OrdinalIgnoreCase))
                    return "#17A2B8"; // Blue
            }
            return "#6C757D"; // Default gray
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}