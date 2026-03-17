#nullable enable
using Microsoft.AspNetCore.SignalR.Client;
using SignalR.EasyUse.Client;
using SkillChat.Interface;
using SkillChat.Server.Domain;
using SkillChat.Server.Test.TestInfrastructure;

namespace SkillChat.Server.Test;

public class ChatHubTests
{
    private static ServerTestHost Host => TestEnvironment.Host;

    [Test]
    public async Task Login_ReturnsOkAndCreatesLoginAudit()
    {
        var user = await Host.CreateUserAsync(displayName: "Tester");
        await using var login = await LoginAsync(Host.CreateAccessToken(user, sessionId: "hub-session"));
        var audit = await Host.LoadAsync<LoginAudit>($"{user.Id}/LoginAudit");

        using var _ = Assert.Multiple();
        await Assert.That(login.Message.Error).IsEqualTo(LogOn.LogOnStatus.Ok);
        await Assert.That(login.Message.Id).IsEqualTo(user.Id);
        await Assert.That(login.Message.UserName).IsEqualTo("Tester");
        await Assert.That(audit).IsNotNull();
        await Assert.That(audit!.SessionId).IsEqualTo("hub-session");
    }

    [Test]
    public async Task Login_ReturnsErrorUserNotFound_WhenUserIsMissing()
    {
        var token = Host.CreateAccessToken(new User
        {
            Id = $"User/{Guid.NewGuid():N}",
            Login = $"missing-{Guid.NewGuid():N}",
        });

        await using var login = await LoginAsync(token);

        await Assert.That(login.Message.Error).IsEqualTo(LogOn.LogOnStatus.ErrorUserNotFound);
    }

    [Test]
    public async Task Login_ReturnsErrorExpiredToken_WhenTokenIsExpired()
    {
        var user = await Host.CreateUserAsync();
        await using var login = await LoginAsync(Host.CreateAccessToken(user, expireIn: TimeSpan.FromSeconds(-5)));

        await Assert.That(login.Message.Error).IsEqualTo(LogOn.LogOnStatus.ErrorExpiredToken);
    }

    [Test]
    public async Task UpdateMyDisplayName_BroadcastsDisplayNameUpdate()
    {
        var actor = await Host.CreateUserAsync(displayName: "Old Name");
        var observer = await Host.CreateUserAsync(displayName: "Observer");
        var chat = await Host.CreateChatAsync(memberIds: [actor.Id, observer.Id]);

        await using var actorConnection = await Host.ConnectHubAsync(actor);
        await using var observerConnection = await Host.ConnectHubAsync(observer);

        var updateSource = new TaskCompletionSource<UpdateUserDisplayName>(TaskCreationOptions.RunContinuationsAsynchronously);
        observerConnection.Connection.Subscribe<UpdateUserDisplayName>(message => updateSource.TrySetResult(message));

        await actorConnection.Hub.UpdateMyDisplayName("New Name");
        var update = await updateSource.Task.WaitAsync(TimeSpan.FromSeconds(10));

        using var _ = Assert.Multiple();
        await Assert.That(chat.Members.Count).IsEqualTo(2);
        await Assert.That(update.Id).IsEqualTo(actor.Id);
        await Assert.That(update.DisplayName).IsEqualTo("New Name");
        await Assert.That(update.UserLogin).IsEqualTo(actor.Login);
    }

    [Test]
    public async Task SendMessage_PersistsMessageAndBroadcastsIt()
    {
        var sender = await Host.CreateUserAsync(displayName: "Sender");
        var receiver = await Host.CreateUserAsync(displayName: "Receiver");
        var chat = await Host.CreateChatAsync(memberIds: [sender.Id, receiver.Id]);

        await using var senderConnection = await Host.ConnectHubAsync(sender);
        await using var receiverConnection = await Host.ConnectHubAsync(receiver);

        var receiveSource = new TaskCompletionSource<ReceiveMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        receiverConnection.Connection.Subscribe<ReceiveMessage>(message => receiveSource.TrySetResult(message));

        await senderConnection.Hub.SendMessage(new HubMessage(chat.Id, "  hello world  ", string.Empty));
        var received = await receiveSource.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var stored = await Host.LoadAsync<Message>(received.Id);

        using var _ = Assert.Multiple();
        await Assert.That(received.ChatId).IsEqualTo(chat.Id);
        await Assert.That(received.UserId).IsEqualTo(sender.Id);
        await Assert.That(received.Text).IsEqualTo("  hello world  ");
        await Assert.That(stored).IsNotNull();
        await Assert.That(stored!.Text).IsEqualTo("hello world");
    }

    [Test]
    public async Task UpdateMessage_UpdatesStoredMessageAndBroadcastsEdit()
    {
        var sender = await Host.CreateUserAsync(displayName: "Sender");
        var receiver = await Host.CreateUserAsync(displayName: "Receiver");
        var chat = await Host.CreateChatAsync(memberIds: [sender.Id, receiver.Id]);
        var quoted = await Host.CreateMessageAsync(chat.Id, receiver.Id, "quoted");
        var original = await Host.CreateMessageAsync(chat.Id, sender.Id, "original");

        await using var senderConnection = await Host.ConnectHubAsync(sender);
        await using var receiverConnection = await Host.ConnectHubAsync(receiver);

        var editedSource = new TaskCompletionSource<ReceiveEditedMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        receiverConnection.Connection.Subscribe<ReceiveEditedMessage>(message => editedSource.TrySetResult(message));

        await senderConnection.Hub.UpdateMessage(new HubEditedMessage(original.Id, chat.Id, " edited text ", quoted.Id));
        var edited = await editedSource.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var stored = await Host.LoadAsync<Message>(original.Id);

        using var _ = Assert.Multiple();
        await Assert.That(edited.Id).IsEqualTo(original.Id);
        await Assert.That(edited.Text).IsEqualTo("edited text");
        await Assert.That(edited.QuotedMessage).IsNotNull();
        await Assert.That(edited.QuotedMessage!.Id).IsEqualTo(quoted.Id);
        await Assert.That(stored!.Text).IsEqualTo("edited text");
        await Assert.That(stored.IdQuotedMessage).IsEqualTo(quoted.Id);
        await Assert.That(stored.LastEditTime).IsNotNull();
    }

    [Test]
    public async Task DeleteMessagesForMe_AddsUserToHiddenList()
    {
        var user = await Host.CreateUserAsync();
        var chat = await Host.CreateChatAsync(memberIds: [user.Id]);
        var message = await Host.CreateMessageAsync(chat.Id, user.Id, "delete me");

        await using var connection = await Host.ConnectHubAsync(user);

        await connection.Hub.DeleteMessagesForMe(new List<string> { message.Id });
        var stored = await Host.LoadAsync<Message>(message.Id);

        await Assert.That(stored!.HideForUsers!.Contains(user.Id)).IsTrue();
    }

    [Test]
    public async Task CleanChatForMe_UpdatesMemberHistoryStart()
    {
        var user = await Host.CreateUserAsync();
        var chat = await Host.CreateChatAsync(memberIds: [user.Id]);

        await using var connection = await Host.ConnectHubAsync(user);

        await connection.Hub.CleanChatForMe(chat.Id);
        var storedChat = await Host.LoadAsync<Chat>(chat.Id);
        var member = storedChat!.Members.Single(m => m.UserId == user.Id);

        await Assert.That(member.MessagesHistoryDateBegin != default(DateTimeOffset)).IsTrue();
    }

    private static async Task<LoginResult> LoginAsync(string token)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl($"{Host.BaseUrl}/chathub")
            .Build();
        var hub = connection.CreateHub<IChatHub>();
        var logOnSource = new TaskCompletionSource<LogOn>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.Subscribe<LogOn>(message => logOnSource.TrySetResult(message));

        await connection.StartAsync();
        await hub.Login(token, "Windows", "127.0.0.1", "SkillChat Tests");
        var logOn = await logOnSource.Task.WaitAsync(TimeSpan.FromSeconds(10));

        return new LoginResult(connection, logOn);
    }

    private sealed record LoginResult(HubConnection Connection, LogOn Message) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Connection.DisposeAsync();
        }
    }
}
