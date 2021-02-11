using System;

namespace SkillChat.Client.ViewModel
{
    public interface INotify
    {
        bool IsClosed { get; }
        int ScreenBottomRightX { get; }
        int ScreenBottomRightY { get; }
        double PrimaryPixelDensity { get; }
        double Width { get; }
        double Height { get; }
        bool ShowInTaskbar { get; set; }
        bool Topmost { get; set; }
        object DataContext { get; set; }
        void Show();
        void Close();
        void SetPosition(int x, int y);
        void OnClosed(Action action);
    }
}