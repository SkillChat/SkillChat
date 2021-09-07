using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SkillChat.Client.Views
{
    public partial class ReplyMessage : UserControl
    {
        public ReplyMessage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
