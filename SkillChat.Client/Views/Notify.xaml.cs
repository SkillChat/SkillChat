using System;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using SkillChat.Client.Notification.ViewModels;
using SkillChat.Client.ViewModel;

namespace SkillChat.Client.Views
{
    public class NotifyWindow :ReactiveWindow<NotifyViewModel>, INotify
    {
        public bool IsClosed { get; private set; }

        public int ScreenBottomRightX => Screens.Primary.WorkingArea.BottomRight.X;

        public int ScreenBottomRightY => Screens.Primary.WorkingArea.BottomRight.Y;

        public void SetPosition(int x, int y)
        {
            Position = new PixelPoint(x,y);
        }

        public void OnClosed(Action action)
        {
            Closed += (sender, args) => action.Invoke();
        }

        public NotifyWindow()
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
