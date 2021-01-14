using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SkillChat.Client.Views.Settings
{
    public class Header : UserControl
    {
        public Header()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
