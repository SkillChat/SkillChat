using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using SkillChat.Client.Notification.ViewModels;

namespace SkillChat.Client.Notification.Views
{
    public class Notify :ReactiveWindow<NotifyViewModel>
    {
        public bool IsClosed { get; private set; }

        public Notify()
        {
           InitializeComponent();
           Closed += delegate { IsClosed = true; };
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
