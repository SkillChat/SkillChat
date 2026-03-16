#nullable enable
using ServiceStack;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.Test.TestInfrastructure;

namespace SkillChat.Server.Test;

public class SettingsServiceTests
{
    private static ServerTestHost Host => TestEnvironment.Host;

    [Test]
    public async Task GetMySettings_CreatesDefaultSettings_WhenTheyDoNotExist()
    {
        var user = await Host.CreateUserAsync();
        var client = Host.CreateClient(Host.CreateAccessToken(user));

        var result = await client.GetAsync(new GetMySettings());
        var storedSettings = await Host.LoadAsync<SkillChat.Server.Domain.Settings>($"{user.Id}/settings");

        using var _ = Assert.Multiple();
        await Assert.That(result.SendingMessageByEnterKey).IsTrue();
        await Assert.That(storedSettings).IsNotNull();
        await Assert.That(storedSettings!.SendingMessageByEnterKey).IsTrue();
    }

    [Test]
    public async Task SetSettings_SavesUserSettings()
    {
        var user = await Host.CreateUserAsync();
        var client = Host.CreateClient(Host.CreateAccessToken(user));

        var result = await client.PostAsync(new SetSettings
        {
            SendingMessageByEnterKey = false,
        });

        var storedSettings = await Host.LoadAsync<SkillChat.Server.Domain.Settings>($"{user.Id}/settings");

        using var _ = Assert.Multiple();
        await Assert.That(result.SendingMessageByEnterKey).IsFalse();
        await Assert.That(storedSettings!.SendingMessageByEnterKey).IsFalse();
    }

    [Test]
    public async Task GetLoginAudit_ReturnsRevisionHistoryAndCurrentSession()
    {
        var user = await Host.CreateUserAsync();

        await using (var firstConnection = await Host.ConnectHubAsync(user, Host.CreateAccessToken(user, sessionId: "session-a")))
        {
        }

        await using (var secondConnection = await Host.ConnectHubAsync(user, Host.CreateAccessToken(user, sessionId: "session-b")))
        {
        }

        var client = Host.CreateClient(Host.CreateAccessToken(user, sessionId: "session-b"));
        var result = await client.GetAsync(new GetLoginAudit());

        using var _ = Assert.Multiple();
        await Assert.That(result.UniqueSessionUser).IsEqualTo("session-b");
        await Assert.That(result.History.Count >= 1).IsTrue();
        await Assert.That(result.History.Any(audit => audit.SessionId == "session-b")).IsTrue();
    }
}
