#nullable enable
using NSubstitute;
using SkillChat.Client.ViewModel;
using SkillChat.Client.ViewModel.Interfaces;
using SkillChat.Client.ViewModel.Services;
using SkillChat.Client.ViewModel.Test.TestInfrastructure;
using Splat;

namespace SkillChat.Client.ViewModel.Test;

public class MessageAndSelectionTests
{
    [Test]
    public async Task SelectMessages_SelectUnselectAndCheckOff_ManageCollection()
    {
        ResetClientState();
        var clipboard = Substitute.For<IClipboard>();
        Locator.CurrentMutable.RegisterConstant(clipboard);

        var selectMessages = new SelectMessages();
        Locator.CurrentMutable.RegisterConstant(selectMessages);
        var message = new MessageViewModel
        {
            UserNickname = "User",
            Text = "Text",
            Time = "10:00",
        };

        selectMessages.Select(message);
        await Assert.That(selectMessages.CountCheckedMsg).IsEqualTo(1);

        selectMessages.UnSelect(message);
        await Assert.That(selectMessages.CountCheckedMsg).IsEqualTo(0);

        selectMessages.Select(message);
        message.IsChecked = true;
        selectMessages.CheckOff();

        using var _ = Assert.Multiple();
        await Assert.That(selectMessages.IsTurnedSelectMode).IsFalse();
        await Assert.That(selectMessages.SelectedCollection.Count).IsEqualTo(0);
        await Assert.That(message.IsChecked).IsFalse();
    }

    [Test]
    public async Task SelectMessages_CopyToClipboardCommand_CopiesMessagesAndTurnsOffMode()
    {
        ResetClientState();
        var clipboard = Substitute.For<IClipboard>();
        Locator.CurrentMutable.RegisterConstant(clipboard);

        var selectMessages = new SelectMessages
        {
            IsTurnedSelectMode = true,
        };
        Locator.CurrentMutable.RegisterConstant(selectMessages);

        selectMessages.SelectedCollection.Add(new MessageViewModel
        {
            UserNickname = "B",
            Text = "Second",
            Time = "11:00",
        });
        selectMessages.SelectedCollection.Add(new MessageViewModel
        {
            UserNickname = "A",
            Text = "First",
            Time = "10:00",
        });

        selectMessages.CopyToClipboardCommand.Execute(null);
        await TestHelpers.EventuallyAsync(() => !selectMessages.IsTurnedSelectMode);

        await clipboard.Received(1).SetTextAsync(Arg.Is<string>(text =>
            text.Contains("A") && text.Contains("First") &&
            text.IndexOf("First", StringComparison.Ordinal) < text.IndexOf("Second", StringComparison.Ordinal)));
    }

    [Test]
    public async Task MessageViewModel_MenuCommands_EditQuoteAndSelectMessage()
    {
        ResetClientState();
        var selectMessages = new SelectMessages();
        var mainWindow = TestHelpers.CreateUninitializedMainWindow();
        mainWindow.SelectMessagesMode = selectMessages;

        Locator.CurrentMutable.RegisterConstant(selectMessages);
        Locator.CurrentMutable.RegisterConstant(mainWindow);
        Locator.CurrentMutable.RegisterConstant<IProfile>(Substitute.For<IProfile>());

        var message = new MessageViewModel
        {
            Id = "message-1",
            Text = "Hello",
            UserId = "user-1",
            UserNickname = "Tester",
            IsMyMessage = true,
        };

        var menuItems = message.MenuItems.ToList();
        menuItems[0].Command.Execute(null);
        menuItems[1].Command.Execute(null);
        menuItems[2].Command.Execute(null);

        using var _ = Assert.Multiple();
        await Assert.That(mainWindow.MessageText).IsEqualTo("Hello");
        await Assert.That(mainWindow.SelectedQuotedMessage).IsSameReferenceAs(message);
        await Assert.That(selectMessages.IsTurnedSelectMode).IsTrue();
        await Assert.That(message.IsChecked).IsTrue();
    }

    [Test]
    public async Task MessageViewModel_UserProfileInfoCommand_OpensProfile()
    {
        ResetClientState();
        var selectMessages = new SelectMessages();
        var profile = Substitute.For<IProfile>();
        Locator.CurrentMutable.RegisterConstant(selectMessages);
        Locator.CurrentMutable.RegisterConstant(profile);

        var message = new MessageViewModel
        {
            UserId = "user-42",
        };

        var openedUserId = string.Empty;
        profile.Open(Arg.Any<string>()).Returns(call =>
        {
            openedUserId = call.Arg<string>();
            return Task.CompletedTask;
        });

        message.UserProfileInfoCommand.Execute("user-42").Subscribe();
        await TestHelpers.EventuallyAsync(() => openedUserId == "user-42");
    }

    [Test]
    public async Task AttachmentMessageViewModel_DownloadCommand_DownloadsThenOpensAttachment()
    {
        ResetClientState();
        var serviceClient = Substitute.For<ISkillChatApiClient>();
        var attachmentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(attachmentPath);
        var attachmentManager = new TrackingAttachmentManager(attachmentPath, serviceClient);
        Locator.CurrentMutable.RegisterConstant(attachmentManager);

        var attachment = TestHelpers.CreateAttachmentMold(fileName: "file.txt", size: 12);
        attachmentManager.DownloadOverride = async () =>
        {
            await File.WriteAllTextAsync(Path.Combine(attachmentPath, attachment.FileName), "hello world!");
            return true;
        };

        var viewModel = new AttachmentMessageViewModel(attachment, attachmentManager);
        viewModel.DownloadCommand.Execute(attachment).Subscribe();
        await TestHelpers.EventuallyAsync(() => viewModel.Text == "Открыть");

        viewModel.DownloadCommand.Execute(attachment).Subscribe();

        using var _ = Assert.Multiple();
        await Assert.That(viewModel.Extensions).IsEqualTo("TXT");
        await Assert.That(viewModel.SizeName).IsNotNull();
        await Assert.That(attachmentManager.LastStartInfo).IsNotNull();
        await Assert.That(attachmentManager.LastStartInfo!.FileName.EndsWith("file.txt")).IsTrue();
    }

    private static void ResetClientState()
    {
        TestHelpers.ResetLocator();
        TestHelpers.ResetNotificationManager();
    }
}
