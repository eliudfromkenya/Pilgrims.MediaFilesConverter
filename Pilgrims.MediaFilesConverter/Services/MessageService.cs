using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using System.Windows.Input;
using DialogHostAvalonia;
using Pilgrims.MediaFilesConverter.Services.Interfaces;

namespace Pilgrims.MediaFilesConverter.Services
{
    public class MessageService : IMessageService
    {
        public async Task ShowMessageAsync(string message, string title = "Information")
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 20,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                            Command = new CloseDialogCommand()
                        }
                    }
                }
            };

            await DialogHost.Show(dialog);
        }

        public async Task ShowErrorAsync(string message, string title = "Error")
        {
            await ShowMessageAsync(message, title);
        }

        public async Task ShowWarningAsync(string message, string title = "Warning")
        {
            await ShowMessageAsync(message, title);
        }

        public async Task<bool> ShowConfirmationAsync(string message, string title = "Confirmation")
        {
            var result = false;
            
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 20,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                            Spacing = 10,
                            Children =
                            {
                                new Button
                                {
                                    Content = "Yes",
                                    Command = new CloseDialogWithResultCommand(() => result = true)
                                },
                                new Button
                                {
                                    Content = "No",
                                    Command = new CloseDialogWithResultCommand(() => result = false)
                                }
                            }
                        }
                    }
                }
            };

            await DialogHost.Show(dialog);
            return result;
        }

        private class CloseDialogCommand : ICommand
        {
            public event EventHandler? CanExecuteChanged;
            
            public bool CanExecute(object? parameter) => true;
            
            public void Execute(object? parameter)
            {
                // Close the current dialog
                if (parameter is Window window)
                {
                    window.Close();
                }
            }
        }

        private class CloseDialogWithResultCommand : ICommand
        {
            private readonly Action _action;
            
            public CloseDialogWithResultCommand(Action action)
            {
                _action = action;
            }
            
            public event EventHandler? CanExecuteChanged;
            
            public bool CanExecute(object? parameter) => true;
            
            public void Execute(object? parameter)
            {
                _action();
                if (parameter is Window window)
                {
                    window.Close();
                }
            }
        }
    }
}