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
        return new FlaUiRuntimeSession(
            DesktopAppSession.Launch(SkillChatAppLaunchHost.CreateDesktopLaunchOptions()));
    }

    protected override MainWindowPage CreatePage(FlaUiRuntimeSession session)
    {
        return new MainWindowPage(
            new FlaUiControlResolver(session.Inner.MainWindow, session.Inner.ConditionFactory));
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
