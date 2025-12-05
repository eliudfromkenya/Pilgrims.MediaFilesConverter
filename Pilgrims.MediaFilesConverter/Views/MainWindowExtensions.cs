using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pilgrims.MediaFilesConverter.Services.Interfaces;
using Pilgrims.MediaFilesConverter.ViewModels;

namespace Pilgrims.MediaFilesConverter.Views
{
    /// <summary>
    /// Extension methods for MainWindow
    /// </summary>
    public static class MainWindowExtensions
    {
        /// <summary>
        /// Open the utility upgrade modal asynchronously
        /// </summary>
        public static async Task OpenUtilityUpgradeModalAsync(this MainWindow mainWindow)
        {
            try
            {
                var messageService = ServiceLocator.GetService<IMessageService>();
                var modal = new UtilityUpgradeModal(messageService);
                await modal.ShowDialog(mainWindow);
            }
            catch (Exception ex)
            {
                var logger = ServiceLocator.GetLogger<MainWindow>();
                logger.LogError(ex, "Failed to open utility upgrade modal");
                var messageService = ServiceLocator.GetService<IMessageService>();
                await messageService.ShowErrorAsync($"Failed to open utility upgrade modal: {ex.Message}", "Error");
            }
        }
    }
}