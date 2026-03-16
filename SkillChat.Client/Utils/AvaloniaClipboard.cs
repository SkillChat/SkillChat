using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace SkillChat.Client.Utils
{
    class AvaloniaClipboard : ViewModel.Interfaces.IClipboard
    {
        public async Task SetTextAsync(string text)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime &&
                lifetime.MainWindow?.Clipboard is { } clipboard)
            {
                await clipboard.SetTextAsync(text);
            }
        }
    }
}
