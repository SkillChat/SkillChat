using AppAutomation.Abstractions;

namespace SkillChat.UiTests.Authoring.Pages;

[UiControl("MainWindowRoot", UiControlType.AutomationElement, "MainWindow")]
[UiControl("LoginPageRoot", UiControlType.AutomationElement, "LoginPage")]
[UiControl("LoginInput", UiControlType.TextBox, "LoginInput")]
[UiControl("LoginPasswordInput", UiControlType.TextBox, "LoginPasswordInput")]
[UiControl("LoginSubmitButton", UiControlType.Button, "LoginSubmitButton")]
[UiControl("GoToRegisterButton", UiControlType.Button, "GoToRegisterButton")]
[UiControl("RegisterPageRoot", UiControlType.AutomationElement, "RegisterPage")]
[UiControl("RegisterLoginInput", UiControlType.TextBox, "RegisterLoginInput")]
[UiControl("RegisterPasswordInput", UiControlType.TextBox, "RegisterPasswordInput")]
[UiControl("RegisterNameInput", UiControlType.TextBox, "RegisterNameInput")]
[UiControl("RegisterConsentCheckBox", UiControlType.CheckBox, "RegisterConsentCheckBox")]
[UiControl("RegisterSubmitButton", UiControlType.Button, "RegisterSubmitButton")]
[UiControl("BackToLoginButton", UiControlType.Button, "BackToLoginButton")]
[UiControl("RegisterErrorLabel", UiControlType.Label, "RegisterErrorLabel")]
public sealed partial class MainWindowPage : UiPage
{
    public MainWindowPage(IUiControlResolver resolver) : base(resolver)
    {
    }
}
