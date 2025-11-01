using Avalonia.Controls;
using Pilgrims.MediaFilesConverter.ViewModels;

namespace Pilgrims.MediaFilesConverter.Controls;

public partial class VideoPreviewControl : UserControl
{
    public VideoPreviewControl()
    {
        InitializeComponent();
        DataContext = new VideoPreviewViewModel();
    }

    public VideoPreviewViewModel? ViewModel => DataContext as VideoPreviewViewModel;

    public void LoadVideo(string videoPath)
    {
        ViewModel?.LoadVideo(videoPath);
    }
}