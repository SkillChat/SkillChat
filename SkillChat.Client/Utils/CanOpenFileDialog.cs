using Avalonia.Controls;
using SkillChat.Interface;
using System.Threading.Tasks;

namespace SkillChat.Client.Utils
{
    public class CanOpenFileDialog : ICanOpenFileDialog
    {
        public async Task<string[]> Open()
        {
            var dialog = new OpenFileDialog();
            dialog.Filters.Add(new FileDialogFilter() { Name = "All", Extensions = { "*" } });
            dialog.AllowMultiple = true;

            return await dialog.ShowAsync(new Window());
        }
    }
}
