using SkillChat.Client.ViewModel.Interfaces;
using Avalonia;
using Avalonia.Input.Platform;

namespace SkillChat.Client.Utils
{
    class SetMessagesToClipboard : IClipboardMessage
    {
        public void SetTextToClipboard(string text)
        {
            AvaloniaLocator.Current.GetService<IClipboard>().SetTextAsync(text.ToString());
        }
    }
}
