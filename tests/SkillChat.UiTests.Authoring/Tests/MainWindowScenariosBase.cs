using System;
using System.Threading;
using AppAutomation.Abstractions;
using AppAutomation.TUnit;
using SkillChat.UiTests.Authoring.Pages;
using TUnit.Assertions;
using TUnit.Core;

namespace SkillChat.UiTests.Authoring.Tests;

public abstract class MainWindowScenariosBase<TSession> : UiTestBase<TSession, MainWindowPage>
    where TSession : class, IUiTestSession
{
    private const int StepTimeoutMs = 10000;

    private static readonly UiWaitOptions WaitOptions = new()
    {
        Timeout = TimeSpan.FromSeconds(10),
        PollInterval = TimeSpan.FromMilliseconds(100)
    };

    [Test]
    [NotInParallel(DesktopUiConstraint)]
    public async Task Login_register_smoke_path_is_reachable()
    {
        WaitUntil(() => Page.LoginPageRoot.AutomationId == "LoginPage",
            "Login page did not become ready.");
        WaitUntil(() => Page.LoginSubmitButton.AutomationId == "LoginSubmitButton",
            "Login submit button did not become ready.");

        Page.ClickButton(static page => page.GoToRegisterButton, timeoutMs: StepTimeoutMs);

        WaitUntil(() => Page.RegisterPageRoot.AutomationId == "RegisterPage",
            "Register page did not become ready.");
        WaitUntil(() => Page.RegisterSubmitButton.AutomationId == "RegisterSubmitButton",
            "Register submit button did not become ready.");

        await Assert.That(Page.RegisterSubmitButton.AutomationId).IsEqualTo("RegisterSubmitButton");
        await Assert.That(Page.RegisterSubmitButton.IsEnabled).IsEqualTo(false);

        Page.SetChecked(static page => page.RegisterConsentCheckBox, true, timeoutMs: StepTimeoutMs);

        await Assert.That(Page.RegisterSubmitButton.IsEnabled).IsEqualTo(true);

        Page.EnterText(static page => page.RegisterLoginInput, "ui-smoke", timeoutMs: StepTimeoutMs);
        Page.EnterText(static page => page.RegisterPasswordInput, "ui-smoke-password", timeoutMs: StepTimeoutMs);
        Page.EnterText(static page => page.RegisterNameInput, "UI Smoke", timeoutMs: StepTimeoutMs);

        Page.ClickButton(static page => page.BackToLoginButton, timeoutMs: StepTimeoutMs);

        WaitUntil(() => Page.LoginPageRoot.AutomationId == "LoginPage",
            "Login page did not restore after register flow.");
        WaitUntil(() => Page.LoginSubmitButton.AutomationId == "LoginSubmitButton",
            "Login submit button did not restore after register flow.");

        await Assert.That(Page.LoginInput.AutomationId).IsEqualTo("LoginInput");
        await Assert.That(Page.LoginPasswordInput.AutomationId).IsEqualTo("LoginPasswordInput");
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
}
