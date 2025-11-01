using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Pilgrims.MediaFilesConverter.ViewModels;

namespace Pilgrims.MediaFilesConverter.Views
{
    public partial class YouTubeDownloadModal : UserControl
    {
        public YouTubeDownloadModal()
        {
            InitializeComponent();
            DataContext = new ViewModels.YouTubeDownloadViewModel();
        }

        private void OnOverlayPressed(object? sender, PointerPressedEventArgs e)
        {           
            // Close modal when clicking outside the content area
            if (DataContext is YouTubeDownloadViewModel viewModel)
            {
                // viewModel.CloseModalCommand.Execute(System.Reactive.Unit.Default);
                //MinWidth = 1000;
                //MinHeight = 700;
            }
        }
    }
}