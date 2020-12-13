using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SkillChat.Client.Views.Profile
{
    public class Settings : UserControl
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}