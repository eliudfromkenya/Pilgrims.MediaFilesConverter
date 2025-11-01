using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FFMpegCore;
using System;
using System.IO;

using Pilgrims.MediaFilesConverter.ViewModels;
using Pilgrims.MediaFilesConverter.Views;
using Pilgrims.MediaFilesConverter.Services;

namespace Pilgrims.MediaFilesConverter;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // Initialize theme service
        var themeService = ThemeService.Instance;
        
        // Configure FFMpegCore with FFmpeg binary path
        ConfigureFFMpeg();
    }
    
    private void ConfigureFFMpeg()
    {
        // First, try to use bundled FFmpeg binaries from application directory
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var bundledFFmpegPath = Path.Combine(appDirectory, "FFmpeg");
        
        var ffmpegPath = Path.Combine(bundledFFmpegPath, "ffmpeg.exe");
        var ffprobePath = Path.Combine(bundledFFmpegPath, "ffprobe.exe");
        
        if (File.Exists(ffmpegPath) && File.Exists(ffprobePath))
        {
            GlobalFFOptions.Configure(new FFOptions { BinaryFolder = bundledFFmpegPath });
            return;
        }
        
        // Fallback: Try to find FFmpeg in common installation paths
        string[] possiblePaths = {
            @"C:\ffmpeg\bin",
            @"C:\Program Files\ffmpeg\bin",
            @"C:\Program Files (x86)\ffmpeg\bin",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ffmpeg", "bin"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin")
        };
        
        foreach (var path in possiblePaths)
        {
            var fallbackFFmpegPath = Path.Combine(path, "ffmpeg.exe");
            var fallbackFFprobePath = Path.Combine(path, "ffprobe.exe");
            
            if (File.Exists(fallbackFFmpegPath) && File.Exists(fallbackFFprobePath))
            {
                GlobalFFOptions.Configure(new FFOptions { BinaryFolder = path });
                return;
            }
        }
        
        // If not found in common paths, try to use PATH environment variable
        // FFMpegCore will use default behavior (search in PATH)
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
