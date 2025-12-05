using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pilgrims.MediaFilesConverter.Services.Interfaces;
using Pilgrims.MediaFilesConverter.ViewModels;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;

namespace Pilgrims.MediaFilesConverter.Views
{
    /// <summary>
    /// Code-behind for UtilityUpgradeModal
    /// </summary>
    public partial class UtilityUpgradeModal : Window
    {
        private readonly UtilityUpgradeViewModel _viewModel;
        private readonly IMessageService _messageService;

        public UtilityUpgradeModal(IMessageService messageService)
        {
            InitializeComponent();
            _messageService = messageService;
            _viewModel = ServiceLocator.GetService<UtilityUpgradeViewModel>();
            DataContext = _viewModel;
        }

        private async void Window_Loaded(object? sender, EventArgs e)
        {
            try
            {
                await _viewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                var logger = ServiceLocator.GetLogger<UtilityUpgradeModal>();
                logger?.LogError(ex, "Error initializing utility upgrade modal");
                await _messageService.ShowErrorAsync($"Error initializing: {ex.Message}", "Initialization Error");
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
            this.Loaded += Window_Loaded;
        }
    }
}