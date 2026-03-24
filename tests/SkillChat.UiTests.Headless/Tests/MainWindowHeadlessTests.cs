using AppAutomation.Avalonia.Headless.Automation;
using AppAutomation.Avalonia.Headless.Session;
using AppAutomation.TUnit;
using SkillChat.AppAutomation.TestHost;
using SkillChat.UiTests.Authoring.Pages;
using SkillChat.UiTests.Authoring.Tests;
using TUnit.Core;

namespace SkillChat.UiTests.Headless.Tests;

[InheritsTests]
public sealed class MainWindowHeadlessTests
    : MainWindowScenariosBase<MainWindowHeadlessTests.HeadlessRuntimeSession>
{
    protected override HeadlessRuntimeSession LaunchSession()
    {
        return new HeadlessRuntimeSession(
            DesktopAppSession.Launch(SkillChatAppLaunchHost.CreateHeadlessLaunchOptions()));
    }

    protected override MainWindowPage CreatePage(HeadlessRuntimeSession session)
    {
        return new MainWindowPage(new HeadlessControlResolver(session.Inner.MainWindow));
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
            Inner.Dispose();
        }
    }
}
