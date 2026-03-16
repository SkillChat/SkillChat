#nullable enable
using SkillChat.Client.ViewModel;

namespace SkillChat.Client.ViewModel.Test.TestInfrastructure;

internal sealed class TestNotifyWindow : INotify
{
    private readonly List<Action> _closedHandlers = new();

    public bool IsClosed { get; private set; }
    public int ScreenBottomRightX { get; set; } = 800;
    public int ScreenBottomRightY { get; set; } = 600;
    public double PrimaryPixelDensity { get; set; } = 1;
    public double Width { get; set; } = 100;
    public double Height { get; set; } = 40;
    public bool ShowInTaskbar { get; set; }
    public bool Topmost { get; set; }
    public object DataContext { get; set; } = null!;
    public bool WasShown { get; private set; }
    public List<(int X, int Y)> Positions { get; } = new();

    public void Show()
    {
        WasShown = true;
    }

    public void Close()
    {
        IsClosed = true;
        foreach (var handler in _closedHandlers.ToArray())
        {
            handler();
        }
    }

    public void SetPosition(int x, int y)
    {
        Positions.Add((x, y));
    }

    public void OnClosed(Action action)
    {
        _closedHandlers.Add(action);
    }
}
