using System.Net;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Web;
using SkillChat.Server.ServiceInterface;

namespace SkillChat.Server.Test;

public class RequestExtensionsTests
{
    [Test]
    public async Task ThrowIfUnauthorized_ReturnsSession_WhenUserIsAuthorized()
    {
        var request = new BasicRequest();
        var session = new AuthUserSession
        {
            UserAuthId = "User/1",
            Id = "session-1",
        };

        request.Items[Keywords.Session] = session;

        var result = request.ThrowIfUnauthorized();

        await Assert.That(result.UserAuthId).IsEqualTo("User/1");
        await Assert.That(result.Id).IsEqualTo("session-1");
    }

    [Test]
    public async Task ThrowIfUnauthorized_ThrowsUnauthorized_WhenUserAuthIdIsMissing()
    {
        var request = new BasicRequest();
        request.Items[Keywords.Session] = new AuthUserSession();

        var exception = await Assert.ThrowsAsync<HttpError>(() => Task.FromResult(request.ThrowIfUnauthorized()));

        await Assert.That(exception!.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
