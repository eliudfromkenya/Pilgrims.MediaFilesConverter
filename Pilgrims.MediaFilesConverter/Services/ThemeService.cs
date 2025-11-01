using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Markup.Xaml.Styling;

namespace Pilgrims.MediaFilesConverter.Services;

public enum AppTheme
{
    Light,
    Dark
}

public class ThemeService : INotifyPropertyChanged
{
    private static ThemeService? _instance;
    public static ThemeService Instance => _instance ??= new ThemeService();

    private AppTheme _currentTheme = AppTheme.Light;

    public AppTheme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                OnPropertyChanged();
                ApplyTheme();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void ApplyTheme()
    {
        if (Application.Current?.Styles is null) return;

        // Clear existing theme styles
        Application.Current.Styles.Clear();

        // Add base styles
        Application.Current.Styles.Add(new StyleInclude(new Uri("avares://Avalonia.Themes.Fluent/FluentTheme.axaml"))
        {
            Source = new Uri("avares://Avalonia.Themes.Fluent/FluentTheme.axaml")
        });

        // Add theme-specific styles
        var themeUri = CurrentTheme == AppTheme.Dark 
            ? "avares://Pilgrims.MediaFilesConverter/Styles/DarkTheme.axaml"
            : "avares://Pilgrims.MediaFilesConverter/Styles/LightTheme.axaml";

        Application.Current.Styles.Add(new StyleInclude(new Uri(themeUri))
        {
            Source = new Uri(themeUri)
        });

        // Add app styles
        Application.Current.Styles.Add(new StyleInclude(new Uri("avares://Pilgrims.MediaFilesConverter/Styles/AppStyles.axaml"))
        {
            Source = new Uri("avares://Pilgrims.MediaFilesConverter/Styles/AppStyles.axaml")
        });
    }

    public void ToggleTheme()
    {
        CurrentTheme = CurrentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}