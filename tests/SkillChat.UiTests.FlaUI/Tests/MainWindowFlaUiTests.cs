using AppAutomation.FlaUI.Automation;
using AppAutomation.FlaUI.Session;
using AppAutomation.TUnit;
using SkillChat.AppAutomation.TestHost;
using SkillChat.UiTests.Authoring.Pages;
using SkillChat.UiTests.Authoring.Tests;
using TUnit.Core;

namespace SkillChat.UiTests.FlaUI.Tests;

[InheritsTests]
public sealed class MainWindowFlaUiTests
    : MainWindowScenariosBase<MainWindowFlaUiTests.FlaUiRuntimeSession>
{
    protected override FlaUiRuntimeSession LaunchSession()
    {
        try
        {
            return new FlaUiRuntimeSession(
                DesktopAppSession.Launch(SkillChatAppLaunchHost.CreateDesktopLaunchOptions()));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("FlaUI AppAutomation launch failed.", ex);
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
            throw new InvalidOperationException("FlaUI AppAutomation page creation failed.", ex);
        }
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
