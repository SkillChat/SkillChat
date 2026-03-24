using AppAutomation.Abstractions;

namespace SkillChat.UiTests.Authoring.Pages;

[UiControl("MainTabs", UiControlType.Tab, "MainTabs")]
[UiControl("SmokeTabItem", UiControlType.TabItem, "SmokeTabItem")]
public sealed partial class MainWindowPage : UiPage
{
    public MainWindowPage(IUiControlResolver resolver) : base(resolver)
    {
    }
}
