using AppAutomation.FlaUI.Automation;
using AppAutomation.FlaUI.Session;
using AppAutomation.Abstractions;
using AppAutomation.TUnit;
using SkillChat.AppAutomation.TestHost;
using SkillChat.Client.Automation;
using SkillChat.UiTests.Authoring.Pages;
using SkillChat.UiTests.Authoring.Tests;
using TUnit.Assertions;
using TUnit.Core;

namespace SkillChat.UiTests.FlaUI.Tests;

[InheritsTests]
public sealed class MainWindowFlaUiSignedInTests
    : MainWindowSignedInScenariosBase<MainWindowFlaUiSignedInTests.FlaUiRuntimeSession>
{
    private static readonly UiWaitOptions WaitOptions = new()
    {
        Timeout = TimeSpan.FromSeconds(10),
        PollInterval = TimeSpan.FromMilliseconds(100)
    };

    protected override FlaUiRuntimeSession LaunchSession()
    {
        try
        {
            return new FlaUiRuntimeSession(
                DesktopAppSession.Launch(
                    SkillChatAppLaunchHost.CreateDesktopLaunchOptions(SkillChatAutomationScenario.SignedInSmoke)));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("FlaUI signed-in AppAutomation launch failed.", ex);
        }
    }

    protected override MainWindowPage CreatePage(FlaUiRuntimeSession session)
    {
        try
        {
            return new MainWindowPage(
                new FlaUiControlResolver(session.Inner.MainWindow, session.Inner.ConditionFactory));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("FlaUI signed-in AppAutomation page creation failed.", ex);
        }
    }

    [Test]
    [NotInParallel(DesktopUiConstraint)]
    public async Task Unread_divider_is_exposed_for_seeded_signed_in_history()
    {
        WaitUntil(
            () => Page.ResolveUnreadDivider("message-unread-1").AutomationId == "UnreadDivider_message-unread-1",
            "Unread divider did not appear for the first unread message.");

        await Assert.That(Page.ResolveUnreadDivider("message-unread-1").AutomationId)
            .IsEqualTo("UnreadDivider_message-unread-1");
    }

    private static void WaitUntil(Func<bool> condition, string timeoutMessage)
    {
        UiWait.Until(
            () =>
            {
                try
                {
                    return condition();
                }
                catch
                {
                    return false;
                }
            },
            static isReady => isReady,
            WaitOptions,
            timeoutMessage,
            CancellationToken.None);
    }

    public sealed class FlaUiRuntimeSession : IUiTestSession
    {
        public FlaUiRuntimeSession(DesktopAppSession inner)
        {
            Inner = inner;
        }

        public DesktopAppSession Inner { get; }

        public void Dispose()
        {
            Inner.Dispose();
        }
    }
}
