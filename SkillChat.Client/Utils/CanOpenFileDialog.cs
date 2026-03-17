using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using SkillChat.Interface;

namespace SkillChat.Client.Utils
{
    public class CanOpenFileDialog : ICanOpenFileDialog
    {
        public async Task<string[]> Open()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime ||
                lifetime.MainWindow?.StorageProvider is not { } storageProvider)
            {
                return [];
            }

            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = true,
                FileTypeFilter = [FilePickerFileTypes.All]
            });

            return files
                .Select(file => file.TryGetLocalPath())
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToArray();
        }
    }
}
