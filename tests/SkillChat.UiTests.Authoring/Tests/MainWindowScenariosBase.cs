using AppAutomation.Abstractions;
using AppAutomation.TUnit;
using SkillChat.UiTests.Authoring.Pages;
using TUnit.Assertions;
using TUnit.Core;

namespace SkillChat.UiTests.Authoring.Tests;

public abstract class MainWindowScenariosBase<TSession> : UiTestBase<TSession, MainWindowPage>
    where TSession : class, IUiTestSession
{
    [Test]
    [NotInParallel(DesktopUiConstraint)]
    public async Task Main_tabs_are_available()
    {
        Page
            .SelectTabItem(static page => page.SmokeTabItem)
            .WaitUntilIsSelected(static page => page.SmokeTabItem);

        await Assert.That(Page.MainTabs.AutomationId).IsEqualTo("MainTabs");
    }
}
