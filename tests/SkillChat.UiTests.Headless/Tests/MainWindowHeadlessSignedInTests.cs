using AppAutomation.Avalonia.Headless.Automation;
using AppAutomation.Avalonia.Headless.Session;
using AppAutomation.Abstractions;
using AppAutomation.TUnit;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using SkillChat.AppAutomation.TestHost;
using SkillChat.Client.Automation;
using SkillChat.Client.ViewModel;
using SkillChat.Client.Views;
using SkillChat.UiTests.Authoring.Pages;
using SkillChat.UiTests.Authoring.Tests;
using TUnit.Core;

namespace SkillChat.UiTests.Headless.Tests;

[InheritsTests]
public sealed class MainWindowHeadlessSignedInTests
    : MainWindowSignedInScenariosBase<MainWindowHeadlessSignedInTests.HeadlessRuntimeSession>
{
    private const double CollapsedSidebarWidth = 48;

    private static readonly UiWaitOptions WaitOptions = new()
    {
        Timeout = TimeSpan.FromSeconds(10),
        PollInterval = TimeSpan.FromMilliseconds(100)
    };

    private string _lastLayoutDiagnostic = "No layout snapshot captured yet.";

    protected override HeadlessRuntimeSession LaunchSession()
    {
        try
        {
            return new HeadlessRuntimeSession(
                DesktopAppSession.Launch(
                    SkillChatAppLaunchHost.CreateHeadlessLaunchOptions(SkillChatAutomationScenario.SignedInSmoke)));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Headless signed-in AppAutomation launch failed.", ex);
        }
    }

    protected override MainWindowPage CreatePage(HeadlessRuntimeSession session)
    {
        try
        {
            return new MainWindowPage(new HeadlessControlResolver(session.Inner.MainWindow));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Headless signed-in AppAutomation page creation failed.", ex);
        }
    }

    [Test]
    [NotInParallel(DesktopUiConstraint)]
    public async Task Sidebar_toggle_reduces_sidebar_width_and_keeps_auto_star_shell_columns()
    {
        WaitUntilChatShellReady();

        var initialLayout = await WaitForLayoutAsync(
            static layout =>
                layout.SidebarWidth > CollapsedSidebarWidth &&
                layout.IsSidebarExpanded &&
                layout.SidebarColumnIsAuto &&
                layout.ContentColumnIsStar,
            "Initial signed-in shell layout did not stabilize.");

        await ToggleSidebarAsync();

        var collapsedLayout = await WaitForLayoutAsync(
            layout =>
                Math.Abs(layout.SidebarWidth - CollapsedSidebarWidth) <= 1 &&
                !layout.IsSidebarExpanded &&
                layout.SidebarColumnIsAuto &&
                layout.ContentColumnIsStar,
            "Sidebar did not collapse to the compact width.");

        using var _ = Assert.Multiple();
        await Assert.That(collapsedLayout.SidebarWidth < initialLayout.SidebarWidth).IsTrue();
        await Assert.That(collapsedLayout.SidebarColumnIsAuto).IsTrue();
        await Assert.That(collapsedLayout.ContentColumnIsStar).IsTrue();
        await Assert.That(Math.Abs(collapsedLayout.SidebarWidth - CollapsedSidebarWidth) <= 1).IsTrue();
    }

    [Test]
    [NotInParallel(DesktopUiConstraint)]
    public async Task Unread_divider_view_binds_visibility_and_automation_id()
    {
        var snapshot = await CaptureUnreadDividerViewSnapshotAsync();

        using var _ = Assert.Multiple();
        await Assert.That(snapshot.IsVisible).IsTrue();
        await Assert.That(snapshot.AutomationId).IsEqualTo("UnreadDivider_message-unread-1");
    }

    public sealed class HeadlessRuntimeSession : IUiTestSession
    {
        public HeadlessRuntimeSession(DesktopAppSession inner)
        {
            Inner = inner;
        }

        public DesktopAppSession Inner { get; }

        public void Dispose()
        {
            try
            {
                ReleaseMainWindow();
            }
            finally
            {
                Inner.Dispose();
            }
        }

        private void ReleaseMainWindow()
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                ReleaseMainWindowCore();
                return;
            }

            Dispatcher.UIThread.Invoke(ReleaseMainWindowCore);
        }

        private void ReleaseMainWindowCore()
        {
            Inner.MainWindow.DataContext = null;
            Inner.MainWindow.Content = null;
            Dispatcher.UIThread.RunJobs();
        }
    }

    private void WaitUntilChatShellReady()
    {
        UiWait.Until(
            () =>
            {
                try
                {
                    return Page.ChatsNavButtonActive.AutomationId == "ChatsNavButtonActive" &&
                           Page.MessageComposerRoot.AutomationId == "MessageComposerRoot";
                }
                catch
                {
                    return false;
                }
            },
            static isReady => isReady,
            WaitOptions,
            "Chat shell did not become ready.",
            CancellationToken.None);
    }

    private async Task<LayoutSnapshot> WaitForLayoutAsync(Func<LayoutSnapshot, bool> condition, string timeoutMessage)
    {
        var stopAt = DateTime.UtcNow.Add(WaitOptions.Timeout);
        while (DateTime.UtcNow < stopAt)
        {
            var layout = await CaptureLayoutSnapshotAsync();
            if (condition(layout))
            {
                return layout;
            }

            await Task.Delay(WaitOptions.PollInterval, CancellationToken.None);
        }

        throw new TimeoutException($"{timeoutMessage} Last observation: {_lastLayoutDiagnostic}");
    }

    private async Task<LayoutSnapshot> CaptureLayoutSnapshotAsync()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return CaptureLayoutSnapshotCore();
        }

        return await Dispatcher.UIThread.InvokeAsync(CaptureLayoutSnapshotCore);
    }

    private async Task<UnreadDividerViewSnapshot> CaptureUnreadDividerViewSnapshotAsync()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return CaptureUnreadDividerViewSnapshotCore();
        }

        return await Dispatcher.UIThread.InvokeAsync(CaptureUnreadDividerViewSnapshotCore);
    }

    private static UnreadDividerViewSnapshot CaptureUnreadDividerViewSnapshotCore()
    {
        var message = new MessageViewModel
        {
            Id = "message-unread-1",
            UserId = "user-smoke-peer",
            UserNickname = "Alice",
            Text = "Unread message",
            PostTime = DateTimeOffset.UtcNow,
            IsUnreadBoundary = true
        };
        var view = new Messages
        {
            DataContext = message
        };

        Dispatcher.UIThread.RunJobs();

        var divider = view.GetLogicalDescendants().OfType<UnreadDivider>().Single();
        var automationId = divider
            .GetLogicalDescendants()
            .OfType<Control>()
            .Select(AutomationProperties.GetAutomationId)
            .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));

        return new UnreadDividerViewSnapshot(divider.IsVisible, automationId ?? string.Empty);
    }

    private async Task ToggleSidebarAsync()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            ToggleSidebarCore();
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(ToggleSidebarCore);
    }

    private LayoutSnapshot CaptureLayoutSnapshotCore()
    {
        try
        {
            var viewModel = Session.Inner.MainWindow.DataContext as MainWindowViewModel;
            var sidebar = FindNamedControl("SidebarRootBorder");
            var sidebarShell = sidebar.Parent as Grid
                               ?? throw new InvalidOperationException(
                                   "Sidebar root is not hosted in the expected shell grid.");
            var chatArea = FindNamedControl("ChatAreaRootGrid");
            var sidebarWidth = ReadSidebarWidth(sidebar);
            _lastLayoutDiagnostic =
                $"vm(expanded={viewModel?.IsSidebarExpanded},sidebarWidth={viewModel?.SidebarWidth:0.##}); " +
                $"sidebar(bounds={sidebar.Bounds.Width:0.##},width={FormatWidth(sidebar.Width)},desired={sidebar.DesiredSize.Width:0.##},visible={sidebar.IsVisible}); " +
                $"shellColumns=({FormatGridLength(sidebarShell.ColumnDefinitions[0].Width)},{FormatGridLength(sidebarShell.ColumnDefinitions[1].Width)}); " +
                $"chat(bounds={chatArea.Bounds.Width:0.##},width={FormatWidth(chatArea.Width)},desired={chatArea.DesiredSize.Width:0.##},visible={chatArea.IsVisible})";
            return new LayoutSnapshot(
                sidebarWidth,
                viewModel?.IsSidebarExpanded ?? false,
                sidebarShell.ColumnDefinitions[0].Width.IsAuto,
                sidebarShell.ColumnDefinitions[1].Width.IsStar);
        }
        catch (Exception ex)
        {
            _lastLayoutDiagnostic = ex.Message;
            return default;
        }
    }

    private void ToggleSidebarCore()
    {
        var viewModel = GetMainWindowViewModel();
        viewModel.ToggleSidebarCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
    }

    private static double ReadSidebarWidth(Control sidebar)
    {
        if (!double.IsNaN(sidebar.Width) && sidebar.Width > 0)
        {
            return sidebar.Width;
        }

        if (sidebar.Bounds.Width > 0)
        {
            return sidebar.Bounds.Width;
        }

        return 0;
    }

    private static string FormatWidth(double width)
    {
        return double.IsNaN(width) ? "NaN" : width.ToString("0.##");
    }

    private static string FormatGridLength(GridLength gridLength)
    {
        if (gridLength.IsAuto)
        {
            return "Auto";
        }

        if (gridLength.IsStar)
        {
            return $"{gridLength.Value:0.##}*";
        }

        return gridLength.Value.ToString("0.##");
    }

    private MainWindowViewModel GetMainWindowViewModel()
    {
        return Session.Inner.MainWindow.DataContext as MainWindowViewModel
               ?? throw new InvalidOperationException("Main window DataContext is not MainWindowViewModel.");
    }

    private Control FindNamedControl(string controlName)
    {
        return Session.Inner.MainWindow.FindControl<Control>(controlName)
               ?? throw new InvalidOperationException(
                   $"Unable to locate named control '{controlName}'.");
    }

    private readonly record struct LayoutSnapshot(
        double SidebarWidth,
        bool IsSidebarExpanded,
        bool SidebarColumnIsAuto,
        bool ContentColumnIsStar);

    private readonly record struct UnreadDividerViewSnapshot(bool IsVisible, string AutomationId);
}
