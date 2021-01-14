using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SkillChat.Client.Views.Settings
{
    public class Settings : UserControl
    {
        public Settings()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
