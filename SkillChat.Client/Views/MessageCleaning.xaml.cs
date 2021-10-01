using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SkillChat.Client.Views
{
    public class MessageCleaning : UserControl
    {
        public MessageCleaning()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
