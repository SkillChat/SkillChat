using System;
using System.Threading;
using AppAutomation.Abstractions;
using AppAutomation.TUnit;
using SkillChat.UiTests.Authoring.Pages;
using TUnit.Assertions;
using TUnit.Core;

namespace SkillChat.UiTests.Authoring.Tests;

public abstract class MainWindowSignedInScenariosBase<TSession> : UiTestBase<TSession, MainWindowPage>
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
    public async Task Signed_in_full_smoke_path_is_reachable()
    {
        WaitUntilChatShellReady();

        await Assert.That(Page.ChatsNavButtonActive.AutomationId).IsEqualTo("ChatsNavButtonActive");
        await Assert.That(Page.AttachMenuButton.AutomationId).IsEqualTo("AttachMenuButton");
        await Assert.That(Page.MessageInput.AutomationId).IsEqualTo("MessageInput");

        await Assert.That(Page.ThemeToggleSwitch.IsToggled).IsEqualTo(true);
        Page.SetToggled(static page => page.ThemeToggleSwitch, false, timeoutMs: StepTimeoutMs);
        WaitUntil(() => Page.ThemeToggleSwitch.IsToggled == false, "Theme toggle did not switch off.");
        Page.SetToggled(static page => page.ThemeToggleSwitch, true, timeoutMs: StepTimeoutMs);
        WaitUntil(() => Page.ThemeToggleSwitch.IsToggled, "Theme toggle did not switch on.");

        Page.ClickButton(static page => page.SidebarToggleButton, timeoutMs: StepTimeoutMs);
        Page.ClickButton(static page => page.SidebarToggleButton, timeoutMs: StepTimeoutMs);
        WaitUntil(() => Page.SettingsNavButton.AutomationId == "SettingsNavButton",
            "Sidebar did not restore expanded navigation.");

        Page.ClickButton(static page => page.ProfileNavButton, timeoutMs: StepTimeoutMs);
        WaitUntil(() => Page.ProfileCloseButton.AutomationId == "ProfileCloseButton",
            "Profile panel did not open.");

        Page.ClickButton(static page => page.ProfileEditNameButton, timeoutMs: StepTimeoutMs);
        WaitUntil(() => Page.ProfileDisplayNameInput.AutomationId == "ProfileDisplayNameInput",
            "Profile display name editor did not open.");

        Page.ClickButton(static page => page.ProfileEditAboutButton, timeoutMs: StepTimeoutMs);
        WaitUntil(() => Page.ProfileAboutMeInput.AutomationId == "ProfileAboutMeInput",
            "Profile about editor did not open.");

        Page.ClickButton(static page => page.ProfileCloseButton, timeoutMs: StepTimeoutMs);
        WaitUntil(() => Page.ChatsNavButtonActive.AutomationId == "ChatsNavButtonActive",
            "Profile panel did not close back to active chats state.");

        Page.ClickButton(static page => page.SettingsNavButton, timeoutMs: StepTimeoutMs);
        WaitUntil(() => Page.SettingsPanelRoot.AutomationId == "SettingsPanelRoot",
            "Settings panel did not open.");
        await Assert.That(Page.SettingsMainTabButton.AutomationId).IsEqualTo("SettingsMainTabButton");

        Page.ClickButton(static page => page.SettingsAuditTabButton, timeoutMs: StepTimeoutMs);
        WaitUntil(() => Page.SettingsAuditListRoot.AutomationId == "SettingsAuditListRoot",
            "Settings audit tab did not open.");
        await Assert.That(Page.SettingsAuditListRoot.AutomationId).IsEqualTo("SettingsAuditListRoot");

        Page.ClickButton(static page => page.ChatsNavButton, timeoutMs: StepTimeoutMs);
        WaitUntilChatShellReady();

        Page.ClickButton(static page => page.HeaderMoreButton, timeoutMs: StepTimeoutMs);
        WaitUntil(() => Page.HeaderSelectMessagesButton.AutomationId == "HeaderSelectMessagesButton",
            "Header context menu did not open.");

        Page.ClickButton(static page => page.HeaderSelectMessagesButton, timeoutMs: StepTimeoutMs);
        WaitUntil(() => Page.SelectionCancelButton.AutomationId == "SelectionCancelButton",
            "Selection toolbar did not open.");
        await Assert.That(Page.SelectionCancelButton.AutomationId).IsEqualTo("SelectionCancelButton");

        Page.ClickButton(static page => page.SelectionCancelButton, timeoutMs: StepTimeoutMs);
        WaitUntil(() => Page.MessageComposerRoot.AutomationId == "MessageComposerRoot",
            "Selection mode did not close.");

        Page.ClickButton(static page => page.HeaderMoreButton, timeoutMs: StepTimeoutMs);
        WaitUntil(() => Page.HeaderClearHistoryButton.AutomationId == "HeaderClearHistoryButton",
            "Header context menu did not reopen.");

        Page.ClickButton(static page => page.HeaderClearHistoryButton, timeoutMs: StepTimeoutMs);
        WaitUntil(() => Page.ConfirmationDialogRoot.AutomationId == "ConfirmationDialogRoot",
            "Confirmation dialog did not open.");

        Page.ClickButton(static page => page.ConfirmationCancelButton, timeoutMs: StepTimeoutMs);
        WaitUntil(() => Page.MessageComposerRoot.AutomationId == "MessageComposerRoot",
            "Confirmation dialog did not close back to chat shell.");

        Page.ClickButton(static page => page.AttachMenuButton, timeoutMs: StepTimeoutMs);
        WaitUntil(() => Page.OpenFileBrowserButton.AutomationId == "OpenFileBrowserButton",
            "Attach menu did not open.");

        Page.ClickButton(static page => page.OpenFileBrowserButton, timeoutMs: StepTimeoutMs);
        WaitUntil(() => Page.AttachmentOverlayRoot.AutomationId == "AttachmentOverlayRoot",
            "Attachment overlay did not open.");
        await Assert.That(Page.AttachmentOverlayList.AutomationId).IsEqualTo("AttachmentOverlayList");

        Page.ClickButton(static page => page.AttachmentOverlayCloseButton, timeoutMs: StepTimeoutMs);
        WaitUntil(() => Page.AttachMenuButton.AutomationId == "AttachMenuButton",
            "Attachment overlay did not close.");
    }

    private void WaitUntilChatShellReady()
    {
        WaitUntil(() => Page.ChatsNavButtonActive.AutomationId == "ChatsNavButtonActive",
            "Chat shell did not become ready.");
        WaitUntil(() => Page.MessageComposerRoot.AutomationId == "MessageComposerRoot",
            "Message composer did not become ready.");
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
