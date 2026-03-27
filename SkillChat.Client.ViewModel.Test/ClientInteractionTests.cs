#nullable enable
using Microsoft.Extensions.Configuration;
using NSubstitute;
using SkillChat.Client.Notification.ViewModels;
using SkillChat.Client.ViewModel;
using SkillChat.Client.ViewModel.Interfaces;
using SkillChat.Client.ViewModel.Services;
using SkillChat.Client.ViewModel.Test.TestInfrastructure;
using SkillChat.Interface;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;
using Splat;

namespace SkillChat.Client.ViewModel.Test;

public class ClientInteractionTests
{
    [Test]
    public async Task SettingsViewModel_Commands_LoadSaveAndCloseSettings()
    {
        ResetClientState();
        var serviceClient = Substitute.For<ISkillChatApiClient>();
        serviceClient.GetMySettingsAsync(Arg.Any<GetMySettings>())
            .Returns(Task.FromResult(new UserChatSettings { SendingMessageByEnterKey = true }));
        serviceClient.SaveSettingsAsync(Arg.Any<SetSettings>())
            .Returns(call => Task.FromResult(new UserChatSettings
            {
                SendingMessageByEnterKey = call.Arg<SetSettings>().SendingMessageByEnterKey
            }));
        serviceClient.GetLoginAuditAsync(Arg.Any<GetLoginAudit>())
            .Returns(Task.FromResult(new LoginHistory
            {
                UniqueSessionUser = "session-1",
                History =
                [
                    new UserLoginAudit
                    {
                        Id = "1",
                        SessionId = "session-1",
                        IpAddress = "127.0.0.1",
                        NameVersionClient = "client",
                        OperatingSystem = "Windows",
                        DateOfEntry = DateTime.Now,
                    }
                ]
            }));

        var viewModel = new SettingsViewModel(serviceClient);

        viewModel.ContextMenuCommand.Execute(null);
        await TestHelpers.EventuallyAsync(() => viewModel.IsContextMenu);

        viewModel.OpenSettingsCommand.Execute(null);
        await TestHelpers.EventuallyAsync(() => viewModel.IsOpened && viewModel.TypeEnter);

        viewModel.TypeEnter = false;
        viewModel.SaveSettingsCommand.Execute(null);
        await TestHelpers.EventuallyAsync(() => viewModel.ChatSettings.SendingMessageByEnterKey == false);

        viewModel.GetHistoryLoginAuditCommand.Execute(null);
        await TestHelpers.EventuallyAsync(() => viewModel.LoginAuditCollection.Count == 1);

        viewModel.CloseContextMenu();
        viewModel.Close();

        using var _ = Assert.Multiple();
        await Assert.That(viewModel.IsContextMenu).IsFalse();
        await Assert.That(viewModel.IsOpened).IsFalse();
        await Assert.That(viewModel.AuditMenuActiveMain).IsTrue();
        await Assert.That(viewModel.LoginAuditCollection[0].IsActive).IsEqualTo("Активный");
    }

    [Test]
    public async Task ProfileViewModel_MethodsAndCommands_LoadAndSaveProfile()
    {
        ResetClientState();
        var serviceClient = Substitute.For<ISkillChatApiClient>();
        serviceClient.GetProfileAsync(Arg.Any<GetProfile>())
            .Returns(Task.FromResult(new UserProfileMold
            {
                Id = "user-1",
                Login = "login",
                DisplayName = "Display",
                AboutMe = "About",
            }));
        serviceClient.SaveProfileAsync(Arg.Any<SetProfile>())
            .Returns(call => Task.FromResult(new UserProfileMold
            {
                Id = "user-1",
                Login = "login",
                DisplayName = call.Arg<SetProfile>().DisplayName,
                AboutMe = call.Arg<SetProfile>().AboutMe,
            }));

        var currentUser = new CurrentUserViewModel { Id = "user-1" };
        var hub = Substitute.For<IChatHub>();
        Locator.CurrentMutable.RegisterConstant<ICurrentUser>(currentUser);

        var viewModel = new ProfileViewModel(serviceClient);
        viewModel.SetChatHub(hub);

        await viewModel.Open("user-1");
        viewModel.LayoutUpdatedWindow.Execute(new TestWindowWidth(600)).Subscribe();
        await TestHelpers.EventuallyAsync(() => !viewModel.IsShowChat);
        viewModel.UpdateUserProfile("Updated", "user-1");
        ((System.Windows.Input.ICommand)viewModel.SetEditNameProfileCommand).Execute(null);
        ((System.Windows.Input.ICommand)viewModel.SetEditAboutMeProfileCommand).Execute(null);
        viewModel.ContextMenuProfile.Execute(null).Subscribe();
        viewModel.DisplayName = "Saved";
        viewModel.AboutMe = "Saved about";
        viewModel.ApplyProfileNameCommand.Execute(null);
        await TestHelpers.EventuallyAsync(() => viewModel.DisplayName == "Saved" && viewModel.AboutMe == "Saved about");
        await hub.Received(1).UpdateMyDisplayName("Saved");

        ((System.Windows.Input.ICommand)viewModel.CloseProfilePanelCommand).Execute(null);
        await TestHelpers.EventuallyAsync(() => !viewModel.IsOpened && viewModel.IsShowChat);

        using var _ = Assert.Multiple();
        await Assert.That(viewModel.IsMyProfile).IsFalse();
        await Assert.That(viewModel.IsOpened).IsFalse();
        await Assert.That(viewModel.IsShowChat).IsTrue();
        await Assert.That(viewModel.DisplayName).IsEqualTo(string.Empty);
        await Assert.That(viewModel.IsActiveContextMenu).IsFalse();
    }

    [Test]
    public async Task SendAttachmentsViewModel_OpenCloseAndSendMessage_Work()
    {
        ResetClientState();
        var serviceClient = Substitute.For<ISkillChatApiClient>();
        serviceClient.UploadAttachment(
                Arg.Any<Stream>(),
                Arg.Any<string>(),
                Arg.Any<SetAttachment>())
            .Returns(call => TestHelpers.CreateAttachmentMold(fileName: call.ArgAt<string>(1)));

        var hub = Substitute.For<IChatHub>();
        var mainWindow = TestHelpers.CreateUninitializedMainWindow();
        mainWindow.ChatId = "chat-1";
        mainWindow.SelectedQuotedMessage = new MessageViewModel { Id = "quoted-1" };

        Locator.CurrentMutable.RegisterConstant(mainWindow);
        Locator.CurrentMutable.RegisterConstant(TestHelpers.CreateClientMapper());

        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(tempFile, "payload");

        try
        {
            var viewModel = new SendAttachmentsViewModel(serviceClient);
            viewModel.SetChatHub(hub);

            await viewModel.Open([tempFile]);
            await viewModel.SendMessage();

            await hub.Received(1).SendMessage(Arg.Is<HubMessage>(message =>
                message.ChatId == "chat-1" &&
                message.IdQuotedMessage == "quoted-1" &&
                message.Attachments.Count == 1 &&
                message.Attachments[0].FileName == Path.GetFileName(tempFile)));

            using var _ = Assert.Multiple();
            await Assert.That(viewModel.IsOpen).IsFalse();
            await Assert.That(viewModel.Attachments.Count).IsEqualTo(1);
            await Assert.That(mainWindow.SelectedQuotedMessage).IsNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public async Task AttachmentManager_Methods_WorkWithFilesAndStreams()
    {
        ResetClientState();
        var attachmentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(attachmentPath);

        var serviceClient = Substitute.For<ISkillChatApiClient>();
        serviceClient.GetAttachmentAsync(Arg.Any<GetAttachment>())
            .Returns(Task.FromResult<Stream>(new MemoryStream("payload"u8.ToArray())));

        var manager = new TrackingAttachmentManager(attachmentPath, serviceClient);
        var attachment = TestHelpers.CreateAttachmentMold(id: "attachment/file-1", fileName: "payload.txt", size: 7);

        var downloaded = await manager.DownloadAttachment(attachment);
        var exists = manager.IsExistAttachment(attachment);
        manager.OpenAttachment(attachment.FileName);

        using var _ = Assert.Multiple();
        await Assert.That(downloaded).IsTrue();
        await Assert.That(exists).IsTrue();
        await Assert.That(File.Exists(Path.Combine(attachmentPath, "payload.txt"))).IsTrue();
        await Assert.That(manager.LastStartInfo).IsNotNull();
    }

    [Test]
    public async Task NotificationAndNotify_ShowWindowAndTruncateText()
    {
        ResetClientState();
        var window = new TestNotifyWindow();
        Locator.CurrentMutable.RegisterConstant<INotify>(window);

        Notification.Manager.Show("VeryLongUserName", "VeryLongMessageText", timeShow: 50);
        await TestHelpers.EventuallyAsync(() => window.WasShown);

        var notifyViewModel = (NotifyViewModel)window.DataContext;
        await Assert.That(notifyViewModel.UserLogin).IsEqualTo("VeryLongUserName");

        window.Close();

        var secondWindow = new TestNotifyWindow();
        Locator.CurrentMutable.RegisterConstant<INotify>(secondWindow);
        Notify.NewMessage("VeryLongUserName", "VeryLongMessageText");
        await TestHelpers.EventuallyAsync(() => secondWindow.WasShown);
        var truncated = (NotifyViewModel)secondWindow.DataContext;

        using var _ = Assert.Multiple();
        await Assert.That(truncated.UserLogin).IsEqualTo("VeryLongUs... : ");
        await Assert.That(truncated.Text).IsEqualTo("\"VeryLongMe...\"");
        await Assert.That(secondWindow.Positions.Count >= 1).IsTrue();
    }

    [Test]
    public async Task MainWindowViewModel_PublicMethods_UpdateState()
    {
        ResetClientState();
        var fileDialog = Substitute.For<ICanOpenFileDialog>();
        var serviceClient = Substitute.For<ISkillChatApiClient>();
        serviceClient.GetProfileAsync(Arg.Any<GetProfile>())
            .Returns(Task.FromResult(new UserProfileMold
            {
                Id = "user-1",
                Login = "login",
                DisplayName = "Display",
                AboutMe = "About",
            }));
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(tempFile, "payload");
        fileDialog.Open().Returns(Task.FromResult(new[] { tempFile }));

        Locator.CurrentMutable.RegisterConstant(fileDialog);
        Locator.CurrentMutable.RegisterConstant(TestHelpers.CreateClientMapper());

        var mainWindow = TestHelpers.CreateUninitializedMainWindow();
        mainWindow.AttachmentViewModel = new SendAttachmentsViewModel(serviceClient);
        mainWindow.SettingsViewModel = new SettingsViewModel(serviceClient) { IsContextMenu = true };
        mainWindow.ProfileViewModel = new ProfileViewModel(serviceClient);

        try
        {
            mainWindow.AttachMenuVisible = true;
            mainWindow.AttachFileClick();
            mainWindow.AttachMenuCommand();
            mainWindow.SelectModeOn();

            var message = new MessageViewModel { Id = "message-1", Text = "Hello" };
            mainWindow.EditMessage(message);
            mainWindow.QuoteMessage(message);
            mainWindow.CancelQuoted();
            mainWindow.User.ErrorMessageLoginPage.GetErrorMessage("500");
            mainWindow.RegisterUser.ErrorMessageRegisterPage.GetErrorMessage("400");
            mainWindow.User.ErrorMessageLoginPage.IsError = true;
            mainWindow.RegisterUser.ErrorMessageRegisterPage.IsError = true;
            mainWindow.ResetErrorCommand();
            mainWindow.WindowStates(MainWindowViewModel.WindowState.SignOut);
            mainWindow.Width(true);
            await mainWindow.OpenFileBrowserMenu();
            await mainWindow.ProfileViewModel.Open("user-1");
            mainWindow.SyncSidebarSelection();
            await Assert.That(mainWindow.IsProfileSelected).IsTrue();
            mainWindow.SettingsViewModel.IsOpened = true;
            mainWindow.SyncSidebarSelection();
            await Assert.That(mainWindow.IsSettingsSelected).IsTrue();
            await Assert.That(mainWindow.IsChatsSelected).IsFalse();
            mainWindow.ShowChats();

            using var _ = Assert.Multiple();
            await Assert.That(mainWindow.AttachMenuVisible).IsFalse();
            await Assert.That(mainWindow.SelectMessagesMode.IsTurnedSelectMode).IsTrue();
            await Assert.That(mainWindow.IsEdited).IsTrue();
            await Assert.That(mainWindow.IsSelectQuotedMessage).IsFalse();
            await Assert.That(mainWindow.User.ErrorMessageLoginPage.IsError).IsFalse();
            await Assert.That(mainWindow.ColumndefinitionWidth).IsEqualTo("*");
            await Assert.That(mainWindow.AttachmentViewModel.IsOpen).IsTrue();
            await Assert.That(mainWindow.IsChatsSelected).IsTrue();
            await Assert.That(mainWindow.IsProfileSelected).IsFalse();
            await Assert.That(mainWindow.IsSettingsSelected).IsFalse();
            await Assert.That(mainWindow.ProfileViewModel.IsOpened).IsFalse();
            await Assert.That(mainWindow.SettingsViewModel.IsOpened).IsFalse();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static void ResetClientState()
    {
        TestHelpers.ResetLocator();
        TestHelpers.ResetNotificationManager();
    }

    private sealed class TestWindowWidth : IHaveWidth
    {
        public TestWindowWidth(double width)
        {
            Width = width;
        }

        public double Width { get; }
    }
}
