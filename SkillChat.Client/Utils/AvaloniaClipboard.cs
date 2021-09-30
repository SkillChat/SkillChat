using System.Threading.Tasks;
using Avalonia;

namespace SkillChat.Client.Utils
{
    class AvaloniaClipboard : ViewModel.Interfaces.IClipboard
    {
        public async Task SetTextAsync(string text)
        {
            await AvaloniaLocator.Current.GetService<Avalonia.Input.Platform.IClipboard>().SetTextAsync(text);
        }
    }
}
