#nullable enable
using ServiceStack;
using SkillChat.Server.Domain;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.Test.TestInfrastructure;

namespace SkillChat.Server.Test;

public class ChatServiceTests
{
    private static ServerTestHost Host => TestEnvironment.Host;

    [Test]
    public async Task GetChatsList_ReturnsOnlyUserChats()
    {
        var user = await Host.CreateUserAsync();
        var anotherUser = await Host.CreateUserAsync();
        var myChat = await Host.CreateChatAsync(chatName: "Mine", memberIds: [user.Id, anotherUser.Id]);
        await Host.CreateChatAsync(chatName: "Hidden", memberIds: [anotherUser.Id]);

        var client = Host.CreateClient(Host.CreateAccessToken(user));
        var result = await client.GetAsync(new GetChatsList());

        using var _ = Assert.Multiple();
        await Assert.That(result.Chats.Count(chat => chat.Id == myChat.Id)).IsEqualTo(1);
        await Assert.That(result.Chats.Any(chat => chat.ChatName == "Hidden")).IsFalse();
    }

    [Test]
    public async Task GetMessages_ReturnsVisibleMessagesWithAttachmentsAndQuotedMessage()
    {
        var user = await Host.CreateUserAsync(displayName: "Sender");
        var quotedAuthor = await Host.CreateUserAsync(displayName: "Quoted");
        var chat = await Host.CreateChatAsync(memberIds: [user.Id, quotedAuthor.Id]);
        var attachment = await Host.CreateAttachmentAsync(user.Id, fileName: "quote.txt");
        var quoted = await Host.CreateMessageAsync(
            chat.Id,
            quotedAuthor.Id,
            "quoted",
            postTime: DateTimeOffset.UtcNow.AddMinutes(-3),
            attachmentIds: new[] { attachment.Id });
        await Host.CreateMessageAsync(
            chat.Id,
            user.Id,
            "hidden",
            postTime: DateTimeOffset.UtcNow.AddMinutes(-2),
            hiddenForUsers: new[] { user.Id });
        var visible = await Host.CreateMessageAsync(
            chat.Id,
            user.Id,
            "visible",
            postTime: DateTimeOffset.UtcNow.AddMinutes(-1),
            quotedMessageId: quoted.Id);

        var client = Host.CreateClient(Host.CreateAccessToken(user));
        var result = await client.GetAsync(new GetMessages
        {
            ChatId = chat.Id,
        });

        var loadedMessage = result.Messages.Single(message => message.Id == visible.Id);

        using var _ = Assert.Multiple();
        await Assert.That(result.Messages.Any(message => message.Text == "hidden")).IsFalse();
        await Assert.That(loadedMessage.QuotedMessage).IsNotNull();
        await Assert.That(loadedMessage.QuotedMessage!.UserNickName).IsEqualTo("Quoted");
        await Assert.That(loadedMessage.QuotedMessage.Attachments.Count).IsEqualTo(1);
        await Assert.That(loadedMessage.QuotedMessage.Attachments[0].FileName).IsEqualTo("quote.txt");
    }

    [Test]
    public async Task GetMessages_ReturnsGlobalFirstUnreadMessageId_AndHasMoreBefore_WhenUnreadRangeExceedsCurrentPage()
    {
        var currentUser = await Host.CreateUserAsync(displayName: "Current");
        var peer = await Host.CreateUserAsync(displayName: "Peer");
        var chat = await Host.CreateChatAsync(memberIds: [currentUser.Id, peer.Id]);

        var readMessage = await Host.CreateMessageAsync(
            chat.Id,
            peer.Id,
            "read",
            postTime: DateTimeOffset.UtcNow.AddMinutes(-10));
        var firstUnread = await Host.CreateMessageAsync(
            chat.Id,
            peer.Id,
            "first unread",
            postTime: DateTimeOffset.UtcNow.AddMinutes(-9));
        await Host.CreateMessageAsync(
            chat.Id,
            peer.Id,
            "unread 2",
            postTime: DateTimeOffset.UtcNow.AddMinutes(-8));
        await Host.CreateMessageAsync(
            chat.Id,
            peer.Id,
            "unread 3",
            postTime: DateTimeOffset.UtcNow.AddMinutes(-7));
        await Host.CreateMessageAsync(
            chat.Id,
            peer.Id,
            "unread 4",
            postTime: DateTimeOffset.UtcNow.AddMinutes(-6));
        await Host.CreateMessageAsync(
            chat.Id,
            peer.Id,
            "unread 5",
            postTime: DateTimeOffset.UtcNow.AddMinutes(-2));

        chat.Members.Single(member => member.UserId == currentUser.Id).LastReadMessagePostTime =
            readMessage.PostTime;
        using (var session = Host.DocumentStore.OpenAsyncSession())
        {
            await session.StoreAsync(chat);
            await session.SaveChangesAsync();
        }

        var client = Host.CreateClient(Host.CreateAccessToken(currentUser));
        var result = await client.GetAsync(new GetMessages
        {
            ChatId = chat.Id,
            PageSize = 2,
        });

        using var _ = Assert.Multiple();
        await Assert.That(result.FirstUnreadMessageId).IsEqualTo(firstUnread.Id);
        await Assert.That(result.HasMoreBefore).IsTrue();
        await Assert.That(result.Messages.Any(message => message.Id == firstUnread.Id)).IsFalse();
    }

    [Test]
    public async Task GetMessages_ReturnsFirstUnreadMessageId_AfterReadMarkerBootstrapOnEmptyChat()
    {
        var currentUser = await Host.CreateUserAsync(displayName: "Current");
        var peer = await Host.CreateUserAsync(displayName: "Peer");
        var chat = await Host.CreateChatAsync(memberIds: [currentUser.Id, peer.Id]);

        await using (var connection = await Host.ConnectHubAsync(currentUser))
        {
            await connection.Hub.MarkChatRead(chat.Id, DateTimeOffset.UtcNow);
        }

        var storedChat = await Host.LoadAsync<Chat>(chat.Id);
        var storedMember = storedChat!.Members.Single(member => member.UserId == currentUser.Id);
        var peerMessage = await Host.CreateMessageAsync(
            chat.Id,
            peer.Id,
            "first unread after bootstrap",
            postTime: storedMember.LastReadMessagePostTime!.Value.AddSeconds(1));

        var client = Host.CreateClient(Host.CreateAccessToken(currentUser));
        MessagePage? result = null;
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (DateTime.UtcNow < deadline)
        {
            result = await client.GetAsync(new GetMessages
            {
                ChatId = chat.Id,
            });

            if (result.Messages.Any(message => message.Id == peerMessage.Id))
            {
                break;
            }

            await Task.Delay(200);
        }

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Messages.Any(message => message.Id == peerMessage.Id)).IsTrue();
        await Assert.That(result.FirstUnreadMessageId).IsEqualTo(peerMessage.Id);
    }
}
