using System;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Pilgrims.MediaFilesConverter.ViewModels;
using Pilgrims.MediaFilesConverter.Models;
using System.ComponentModel;
using System.Diagnostics;

namespace Pilgrims.MediaFilesConverter.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    private void OnYouTubeButtonClick(object? sender, RoutedEventArgs e)
    {
        Debug.WriteLine("YouTube button clicked!");
        if (DataContext is MainViewModel viewModel)
        {
            Debug.WriteLine("MainViewModel found, executing OpenYouTubeModalCommand");
            viewModel.OpenYouTubeModalCommand?.Execute(Unit.Default);
        }
        else
        {
            Debug.WriteLine("MainViewModel not found in DataContext");
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        // Only allow if the data object contains file list
        if (e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null && DataContext is MainViewModel viewModel)
            {
                var filePaths = files.Select(f => f.Path.LocalPath).ToArray();
                viewModel.AddFiles(filePaths);
            }
        }
    }

    private void OnDropAreaDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            // Trigger the AddFilesCommand which will open the file picker
            viewModel.AddFilesCommand.Execute(Unit.Default);
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // Wire up file picker for Add Files command
        if (DataContext is MainViewModel viewModel)
        {
            // Subscribe to SelectedMediaFile changes to update video preview
            viewModel.PropertyChanged += OnViewModelPropertyChanged;

            viewModel.AddFilesCommand.Subscribe(async _ =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "Select Media Files",
                        AllowMultiple = true,
                        FileTypeFilter = new[]
                        {
                            new FilePickerFileType("Video Files")
                            {
                                Patterns = new[] { "*.mp4", "*.avi", "*.mov", "*.mkv", "*.wmv", "*.flv", "*.webm", "*.m4v" }
                            },
                            new FilePickerFileType("Audio Files")
                            {
                                Patterns = new[] { "*.mp3", "*.wav", "*.flac", "*.aac", "*.ogg", "*.wma", "*.m4a" }
                            },
                            new FilePickerFileType("All Media Files")
                            {
                                Patterns = new[] { "*.mp4", "*.avi", "*.mov", "*.mkv", "*.wmv", "*.flv", "*.webm", "*.m4v", 
                                                 "*.mp3", "*.wav", "*.flac", "*.aac", "*.ogg", "*.wma", "*.m4a" }
                            }
                        }
                    });

                    if (files.Count > 0)
                    {
                        var filePaths = files.Select(f => f.Path.LocalPath).ToArray();
                        viewModel.AddFiles(filePaths);
                    }
                }
            });

            viewModel.SelectOutputDirectoryCommand.Subscribe(async _ =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                    {
                        Title = "Select Output Directory",
                        AllowMultiple = false
                    });

                    if (folders.Count > 0)
                    {
                        viewModel.ConversionSettings.OutputDirectory = folders[0].Path.LocalPath;
                    }
                }
            });
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedMediaFile) && 
            DataContext is MainViewModel viewModel)
        {
            UpdateVideoPreview(viewModel.SelectedMediaFile);
        }
    }

    private void UpdateVideoPreview(MediaFile? selectedFile)
    {
        if (selectedFile != null && selectedFile.FileType == MediaFileType.Video)
        {
            VideoPreview.LoadVideo(selectedFile.FilePath);
        }
    }
}
