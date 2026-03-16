#nullable enable
using ServiceStack;
using SkillChat.Server.Domain;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.Test.TestInfrastructure;

namespace SkillChat.Server.Test;

public class ProfileServiceTests
{
    private static ServerTestHost Host => TestEnvironment.Host;

    [Test]
    public async Task GetProfile_ReturnsRequestedUserProfile()
    {
        var user = await Host.CreateUserAsync(displayName: "Visible User", aboutMe: "About");
        var requester = await Host.CreateUserAsync();
        var client = Host.CreateClient(Host.CreateAccessToken(requester));

        var result = await client.GetAsync(new GetProfile
        {
            UserId = user.Id,
        });

        using var _ = Assert.Multiple();
        await Assert.That(result.Id).IsEqualTo(user.Id);
        await Assert.That(result.Login).IsEqualTo(user.Login);
        await Assert.That(result.DisplayName).IsEqualTo("Visible User");
        await Assert.That(result.AboutMe).IsEqualTo("About");
    }

    [Test]
    public async Task SetProfile_UpdatesProfileAndTruncatesDisplayName()
    {
        var user = await Host.CreateUserAsync(displayName: "Old", aboutMe: "Old about");
        var client = Host.CreateClient(Host.CreateAccessToken(user));
        var tooLongName = new string('A', 40);

        var result = await client.PostAsync(new SetProfile
        {
            DisplayName = tooLongName,
            AboutMe = "Updated about me",
        });

        var savedUser = await Host.LoadAsync<User>(user.Id);

        using var _ = Assert.Multiple();
        await Assert.That(result.DisplayName.Length).IsEqualTo(32);
        await Assert.That(result.AboutMe).IsEqualTo("Updated about me");
        await Assert.That(savedUser!.DisplayName).IsEqualTo(new string('A', 32));
        await Assert.That(savedUser.AboutMe).IsEqualTo("Updated about me");
    }
}
