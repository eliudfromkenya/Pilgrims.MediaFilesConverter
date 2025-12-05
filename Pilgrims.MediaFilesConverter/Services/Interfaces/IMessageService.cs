using System.Threading.Tasks;

namespace Pilgrims.MediaFilesConverter.Services.Interfaces
{
    public interface IMessageService
    {
        Task ShowMessageAsync(string message, string title = "Information");
        Task ShowErrorAsync(string message, string title = "Error");
        Task ShowWarningAsync(string message, string title = "Warning");
        Task<bool> ShowConfirmationAsync(string message, string title = "Confirmation");
    }
}