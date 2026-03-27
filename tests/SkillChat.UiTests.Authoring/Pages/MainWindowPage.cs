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
[UiControl("SignedInShellRoot", UiControlType.AutomationElement, "SignedInShellRoot")]
[UiControl("SidebarRoot", UiControlType.AutomationElement, "SidebarRoot")]
[UiControl("SidebarToggleButton", UiControlType.Button, "SidebarToggleButton")]
[UiControl("ChatsNavButton", UiControlType.Button, "ChatsNavButton")]
[UiControl("ChatsNavButtonActive", UiControlType.Button, "ChatsNavButtonActive")]
[UiControl("ProfileNavButton", UiControlType.Button, "ProfileNavButton")]
[UiControl("ProfileNavButtonActive", UiControlType.Button, "ProfileNavButtonActive")]
[UiControl("SettingsNavButton", UiControlType.Button, "SettingsNavButton")]
[UiControl("SettingsNavButtonActive", UiControlType.Button, "SettingsNavButtonActive")]
[UiControl("ThemeToggleSwitch", UiControlType.ToggleButton, "ThemeToggleSwitch")]
[UiControl("SettingsPanelHost", UiControlType.AutomationElement, "SettingsPanelHost")]
[UiControl("SettingsPanelRoot", UiControlType.AutomationElement, "SettingsPanelRoot")]
[UiControl("SettingsMainTabButton", UiControlType.Button, "SettingsMainTabButton")]
[UiControl("SettingsAuditTabButton", UiControlType.Button, "SettingsAuditTabButton")]
[UiControl("SettingsMainContent", UiControlType.AutomationElement, "SettingsMainContent")]
[UiControl("SettingsAuditContent", UiControlType.AutomationElement, "SettingsAuditContent")]
[UiControl("SettingsAuditListRoot", UiControlType.AutomationElement, "SettingsAuditListRoot")]
[UiControl("ChatAreaRoot", UiControlType.AutomationElement, "ChatAreaRoot")]
[UiControl("MessagesRegion", UiControlType.AutomationElement, "MessagesRegion")]
[UiControl("MessagesScrollViewer", UiControlType.AutomationElement, "MessagesScrollViewer")]
[UiControl("MessagesListRoot", UiControlType.AutomationElement, "MessagesListRoot")]
[UiControl("MessageComposerRoot", UiControlType.AutomationElement, "MessageComposerRoot")]
[UiControl("AttachMenuButton", UiControlType.Button, "AttachMenuButton")]
[UiControl("MessageInput", UiControlType.TextBox, "MessageInput")]
[UiControl("MessageSendButton", UiControlType.Button, "MessageSendButton")]
[UiControl("AttachMenuRoot", UiControlType.AutomationElement, "AttachMenuRoot")]
[UiControl("AttachFileMenuButton", UiControlType.Button, "AttachFileMenuButton")]
[UiControl("OpenFileBrowserButton", UiControlType.Button, "OpenFileBrowserButton")]
[UiControl("HeaderMoreButton", UiControlType.Button, "HeaderMoreButton")]
[UiControl("HeaderContextMenuRoot", UiControlType.AutomationElement, "HeaderContextMenuRoot")]
[UiControl("HeaderSelectMessagesButton", UiControlType.Button, "HeaderSelectMessagesButton")]
[UiControl("HeaderClearHistoryButton", UiControlType.Button, "HeaderClearHistoryButton")]
[UiControl("HeaderOpenSettingsButton", UiControlType.Button, "HeaderOpenSettingsButton")]
[UiControl("SelectionToolbarRoot", UiControlType.AutomationElement, "SelectionToolbarRoot")]
[UiControl("SelectionCopyButton", UiControlType.Button, "SelectionCopyButton")]
[UiControl("SelectionDeleteButton", UiControlType.Button, "SelectionDeleteButton")]
[UiControl("SelectionCountLabel", UiControlType.Label, "SelectionCountLabel")]
[UiControl("SelectionCancelButton", UiControlType.Button, "SelectionCancelButton")]
[UiControl("ConfirmationDialogRoot", UiControlType.AutomationElement, "ConfirmationDialogRoot")]
[UiControl("ConfirmationCloseButton", UiControlType.Button, "ConfirmationCloseButton")]
[UiControl("ConfirmationCancelButton", UiControlType.Button, "ConfirmationCancelButton")]
[UiControl("ConfirmationConfirmButton", UiControlType.Button, "ConfirmationConfirmButton")]
[UiControl("AttachmentOverlayRoot", UiControlType.AutomationElement, "AttachmentOverlayRoot")]
[UiControl("AttachmentOverlayList", UiControlType.ListBox, "AttachmentOverlayList")]
[UiControl("AttachmentOverlayCloseButton", UiControlType.Button, "AttachmentOverlayCloseButton")]
[UiControl("AttachmentOverlayMessageInput", UiControlType.TextBox, "AttachmentOverlayMessageInput")]
[UiControl("AttachmentOverlaySendButton", UiControlType.Button, "AttachmentOverlaySendButton")]
[UiControl("ProfilePanelHost", UiControlType.AutomationElement, "ProfilePanelHost")]
[UiControl("ProfilePanelRoot", UiControlType.AutomationElement, "ProfilePanelRoot")]
[UiControl("ProfileCloseButton", UiControlType.Button, "ProfileCloseButton")]
[UiControl("ProfileDisplayNameInput", UiControlType.TextBox, "ProfileDisplayNameInput")]
[UiControl("ProfileEditNameButton", UiControlType.Button, "ProfileEditNameButton")]
[UiControl("ProfileSaveNameButton", UiControlType.Button, "ProfileSaveNameButton")]
[UiControl("ProfileAboutMeInput", UiControlType.TextBox, "ProfileAboutMeInput")]
[UiControl("ProfileEditAboutButton", UiControlType.Button, "ProfileEditAboutButton")]
[UiControl("ProfileSaveAboutButton", UiControlType.Button, "ProfileSaveAboutButton")]
[UiControl("ProfileSignOutButton", UiControlType.Button, "ProfileSignOutButton")]
public sealed partial class MainWindowPage : UiPage
{
    private readonly IUiControlResolver _resolver;

    public MainWindowPage(IUiControlResolver resolver) : base(resolver)
    {
        _resolver = resolver;
    }

    public IUiControl ResolveMessageItem(string messageId) =>
        Resolve<IUiControl>(
            $"MessageItem_{messageId}",
            UiControlType.AutomationElement,
            $"{nameof(ResolveMessageItem)}_{messageId}");

    public ICheckBoxControl ResolveMessageCheckbox(string messageId) =>
        Resolve<ICheckBoxControl>(
            $"MessageCheckbox_{messageId}",
            UiControlType.CheckBox,
            $"{nameof(ResolveMessageCheckbox)}_{messageId}");

    private TControl Resolve<TControl>(string automationId, UiControlType controlType, string propertyName)
        where TControl : class, IUiControl
    {
        return _resolver.Resolve<TControl>(new UiControlDefinition(
            propertyName,
            controlType,
            automationId,
            UiLocatorKind.AutomationId,
            FallbackToName: false));
    }
}
