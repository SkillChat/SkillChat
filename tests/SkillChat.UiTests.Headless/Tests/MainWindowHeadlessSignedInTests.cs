using AppAutomation.Avalonia.Headless.Automation;
using AppAutomation.Avalonia.Headless.Session;
using AppAutomation.TUnit;
using SkillChat.AppAutomation.TestHost;
using SkillChat.Client.Automation;
using SkillChat.UiTests.Authoring.Pages;
using SkillChat.UiTests.Authoring.Tests;
using TUnit.Core;

namespace SkillChat.UiTests.Headless.Tests;

[InheritsTests]
public sealed class MainWindowHeadlessSignedInTests
    : MainWindowSignedInScenariosBase<MainWindowHeadlessSignedInTests.HeadlessRuntimeSession>
{
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
