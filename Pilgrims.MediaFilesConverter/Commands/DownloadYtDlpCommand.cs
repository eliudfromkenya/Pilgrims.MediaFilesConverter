using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Pilgrims.MediaFilesConverter.Services;

namespace Pilgrims.MediaFilesConverter.Commands
{
    /// <summary>
    /// Command to download yt-dlp executable
    /// </summary>
    public class DownloadYtDlpCommand : ICommand
    {
        private readonly YtDlpManager _ytDlpManager;
        private bool _isExecuting;

        public DownloadYtDlpCommand()
        {
            _ytDlpManager = YtDlpManager.Instance;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting;
        }

        public async void Execute(object? parameter)
        {
            if (_isExecuting)
                return;

            _isExecuting = true;
            OnCanExecuteChanged();

            try
            {
                var progress = parameter as IProgress<string>;
                
                progress?.Report("Starting yt-dlp download...");
                
                var success = await _ytDlpManager.DownloadYtDlpAsync(progress);
                
                if (success)
                {
                    progress?.Report("yt-dlp downloaded successfully!");
                    DownloadCompleted?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    progress?.Report("Failed to download yt-dlp. Please try downloading manually from https://github.com/yt-dlp/yt-dlp/releases");
                }
            }
            catch (Exception ex)
            {
                var progress = parameter as IProgress<string>;
                progress?.Report($"Error downloading yt-dlp: {ex.Message}");
            }
            finally
            {
                _isExecuting = false;
                OnCanExecuteChanged();
            }
        }

        protected virtual void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? DownloadCompleted;
    }
}