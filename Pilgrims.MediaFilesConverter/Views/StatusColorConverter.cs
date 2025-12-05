using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Pilgrims.MediaFilesConverter.Views
{
    /// <summary>
    /// Converts status strings to corresponding colors
    /// </summary>
    public class StatusColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLowerInvariant() switch
                {
                    "error" => "#FF0000", // Red
                    "available" or "up to date" => "#00FF00", // Green
                    "checking" or "starting" => "#FFFF00", // Yellow
                    "completed" => "#0000FF", // Blue
                    _ => "#808080" // Gray
                };
            }
            return "#808080"; // Gray
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}