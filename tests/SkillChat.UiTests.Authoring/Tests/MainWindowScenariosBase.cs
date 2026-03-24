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
    public async Task Login_register_smoke_path_is_reachable()
    {
        Page.WaitUntilNameEquals(static page => page.LoginSubmitButton, "Войти", timeoutMs: 5000);

        Page.ClickButton(static page => page.GoToRegisterButton, timeoutMs: 5000);

        Page.WaitUntilNameEquals(static page => page.RegisterSubmitButton, "Зарегистрироваться", timeoutMs: 5000);

        await Assert.That(Page.RegisterSubmitButton.AutomationId).IsEqualTo("RegisterSubmitButton");
        await Assert.That(Page.RegisterSubmitButton.IsEnabled).IsEqualTo(false);

        Page.SetChecked(static page => page.RegisterConsentCheckBox, true, timeoutMs: 5000);

        await Assert.That(Page.RegisterSubmitButton.IsEnabled).IsEqualTo(true);

        Page.EnterText(static page => page.RegisterLoginInput, "ui-smoke", timeoutMs: 5000);
        Page.EnterText(static page => page.RegisterPasswordInput, "ui-smoke-password", timeoutMs: 5000);
        Page.EnterText(static page => page.RegisterNameInput, "UI Smoke", timeoutMs: 5000);

        Page.ClickButton(static page => page.BackToLoginButton, timeoutMs: 5000);

        Page.WaitUntilNameEquals(static page => page.LoginSubmitButton, "Войти", timeoutMs: 5000);

        await Assert.That(Page.LoginInput.AutomationId).IsEqualTo("LoginInput");
        await Assert.That(Page.LoginPasswordInput.AutomationId).IsEqualTo("LoginPasswordInput");
    }
}
