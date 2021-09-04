using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SkillChat.Client.Views
{
    public partial class Message : UserControl
    {
        public Message()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
