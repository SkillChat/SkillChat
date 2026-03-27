using System;
using System.Linq;
using System.Threading.Tasks;
using SkillChat.Client.ViewModel;
using SkillChat.Client.ViewModel.Interfaces;
using SkillChat.Interface;

namespace SkillChat.Client.Automation;

internal sealed class SkillChatAutomationClipboard : IClipboard
{
    public string LastText { get; private set; } = string.Empty;

    public Task SetTextAsync(string text)
    {
        LastText = text ?? string.Empty;
        return Task.CompletedTask;
    }
}

internal sealed class SkillChatAutomationFileDialog : ICanOpenFileDialog
{
    private readonly SkillChatAutomationState _state;

    public SkillChatAutomationFileDialog(SkillChatAutomationState state)
    {
        _state = state;
    }

    public Task<string[]> Open() => Task.FromResult(_state.FileDialogSelection.ToArray());
}

internal sealed class SkillChatAutomationNotifyWindow : INotify
{
    private Action? _closed;

    public bool IsClosed { get; private set; } = true;

    public int ScreenBottomRightX { get; private set; }

    public int ScreenBottomRightY { get; private set; }

    public double PrimaryPixelDensity { get; private set; } = 1d;

    public double Width { get; private set; } = 320d;

    public double Height { get; private set; } = 96d;

    public bool ShowInTaskbar { get; set; }

    public bool Topmost { get; set; }

    public object DataContext { get; set; } = new object();

    public void Show()
    {
        IsClosed = false;
    }

    public void Close()
    {
        IsClosed = true;
        _closed?.Invoke();
    }

    public void SetPosition(int x, int y)
    {
        ScreenBottomRightX = x;
        ScreenBottomRightY = y;
    }

    public void OnClosed(Action action)
    {
        _closed = action;
    }
}
