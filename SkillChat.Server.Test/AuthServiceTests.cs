#nullable enable
using ServiceStack;
using SkillChat.Server.Domain;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.Test.TestInfrastructure;

namespace SkillChat.Server.Test;

public class AuthServiceTests
{
    private static ServerTestHost Host => TestEnvironment.Host;

    [Test]
    public async Task RegisterNewUser_CreatesUserSecretAndDefaultChatMembership()
    {
        var client = Host.CreateClient();
        var login = $"register-{Guid.NewGuid():N}";

        var result = await client.PostAsync(new RegisterNewUser
        {
            Login = login,
            Password = "Password123!",
            UserName = "Neo",
        });

        var user = await Host.FindUserByLoginAsync(login);
        var secret = await Host.LoadAsync<UserSecret>($"{user!.Id}/secret");
        var defaultChat = await Host.FindChatByNameAsync("SkillBoxChat");

        using var _ = Assert.Multiple();
        await Assert.That(result.AccessToken).IsNotNull();
        await Assert.That(result.RefreshToken).IsNotNull();
        await Assert.That(user).IsNotNull();
        await Assert.That(user!.DisplayName).IsEqualTo("Neo");
        await Assert.That(secret).IsNotNull();
        await Assert.That(secret!.Password).IsNotEqualTo("Password123!");
        await Assert.That(defaultChat!.Members.Any(member => member.UserId == user.Id)).IsTrue();
    }

    [Test]
    public async Task AuthViaPassword_ReturnsTokenPair_ForValidCredentials()
    {
        var user = await Host.CreateUserAsync(password: "Password123!");
        var client = Host.CreateClient();

        TokenResult? result = null;
        Exception? lastException = null;

        for (var attempt = 0; attempt < 10 && result is null; attempt++)
        {
            try
            {
                result = await client.PostAsync(new AuthViaPassword
                {
                    Login = user.Login,
                    Password = "Password123!",
                });
            }
            catch (WebServiceException exception) when (exception.StatusCode is >= 500 or 404)
            {
                lastException = exception;
                await Task.Delay(100);
            }
        }

        if (result is null)
        {
            throw lastException ?? new InvalidOperationException("The auth request never completed successfully.");
        }

        using var _ = Assert.Multiple();
        await Assert.That(result.AccessToken).IsNotNull();
        await Assert.That(result.RefreshToken).IsNotNull();
    }

    [Test]
    public async Task AuthViaPassword_ThrowsNotFound_ForInvalidPassword()
    {
        var user = await Host.CreateUserAsync(password: "Password123!");
        var client = Host.CreateClient();

        var exception = await Assert.ThrowsAsync<WebServiceException>(() => client.PostAsync(new AuthViaPassword
        {
            Login = user.Login,
            Password = "wrong-password",
        }));

        await Assert.That(exception!.StatusCode).IsEqualTo(404);
    }

    [Test]
    public async Task PostRefreshToken_ReturnsNewTokenPair_ForRefreshToken()
    {
        var user = await Host.CreateUserAsync(password: "Password123!");
        var client = Host.CreateClient(Host.CreateRefreshToken(user));

        var result = await client.PostAsync(new PostRefreshToken());

        using var _ = Assert.Multiple();
        await Assert.That(result.AccessToken).IsNotNull();
        await Assert.That(result.RefreshToken).IsNotNull();
        await Assert.That(result.AccessToken).IsNotEqualTo(client.BearerToken);
    }

    [Test]
    public async Task CreatePassword_CreatesHashForUserWithoutSecret()
    {
        var user = await Host.CreateUserAsync(password: null);
        var client = Host.CreateClient(Host.CreateAccessToken(user));

        var result = await client.PostAsync(new CreatePassword
        {
            NewPassword = "StrongPassword42!",
        });

        var authClient = Host.CreateClient();
        var authResult = await authClient.PostAsync(new AuthViaPassword
        {
            Login = user.Login,
            Password = "StrongPassword42!",
        });

        using var _ = Assert.Multiple();
        await Assert.That(result.Result).IsEqualTo(PasswordChangeResult.ChangeEnum.Created);
        await Assert.That(authResult.AccessToken).IsNotNull();
    }

    [Test]
    public async Task GetMyProfile_ReturnsCurrentUserProfile()
    {
        var user = await Host.CreateUserAsync(password: "Password123!", displayName: "Tester", aboutMe: "About me");
        var client = Host.CreateClient(Host.CreateAccessToken(user));

        var result = await client.GetAsync(new GetMyProfile());

        using var _ = Assert.Multiple();
        await Assert.That(result.Id).IsEqualTo(user.Id);
        await Assert.That(result.Login).IsEqualTo(user.Login);
        await Assert.That(result.DisplayName).IsEqualTo("Tester");
        await Assert.That(result.AboutMe).IsEqualTo("About me");
        await Assert.That(result.IsPasswordSetted).IsTrue();
    }
}
