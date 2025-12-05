using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Pilgrims.MediaFilesConverter.Services.Interfaces;
using Pilgrims.MediaFilesConverter.ViewModels;

namespace Pilgrims.MediaFilesConverter.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void OpenUtilityUpgradeModal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await this.OpenUtilityUpgradeModalAsync();
            }
            catch (Exception ex)
            {
                var messageService = ServiceLocator.GetService<IMessageService>();
                await messageService.ShowErrorAsync($"Error opening utility upgrade manager: {ex.Message}", "Error");
            }
        }
    }
}
