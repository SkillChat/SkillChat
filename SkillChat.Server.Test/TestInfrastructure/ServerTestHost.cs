#nullable enable
using AutoMapper;
using AutoMapper.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using ServiceStack;
using ServiceStack.Auth;
using SignalR.EasyUse.Client;
using SkillChat.Interface;
using SkillChat.Server.Domain;
using SkillChat.Server.ServiceInterface;

namespace SkillChat.Server.Test.TestInfrastructure;

internal sealed class ServerTestHost : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly string _rootDirectory;

    private ServerTestHost(
        IHost host,
        string rootDirectory,
        string baseUrl,
        string filesPath,
        IDocumentStore documentStore)
    {
        _host = host;
        _rootDirectory = rootDirectory;
        BaseUrl = baseUrl;
        FilesPath = filesPath;
        DocumentStore = documentStore;
    }

    public string BaseUrl { get; }
    public string FilesPath { get; }
    public IDocumentStore DocumentStore { get; }

    public static async Task<ServerTestHost> StartAsync()
    {
        var basePort = PortHelper.GetFreeTcpPort();
        var ravenPort = PortHelper.GetFreeTcpPort();

        var serverProjectDirectory = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "SkillChat.Server"));

        var rootDirectory = Path.Combine(
            Path.GetTempPath(),
            "SkillChat.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        var dataDirectory = Path.Combine(rootDirectory, "RavenDB");
        var logsDirectory = Path.Combine(rootDirectory, "Logs");
        var filesDirectory = Path.Combine(rootDirectory, "Files");
        var baseUrl = $"http://127.0.0.1:{basePort}";
        var overrides = new Dictionary<string, string?>
        {
            ["Security:RequireSecureConnection"] = "false",
            ["RavenDb:DatabaseRecord:DatabaseName"] = $"SkillChatTests-{Guid.NewGuid():N}",
            ["RavenDb:ServerOptions:ServerUrl"] = $"http://127.0.0.1:{ravenPort}/",
            ["RavenDb:ServerOptions:FrameworkVersion"] = "10.0.0",
            ["RavenDb:ServerOptions:DataDirectory"] = dataDirectory,
            ["RavenDb:ServerOptions:LogsPath"] = logsDirectory,
            ["FilesPath"] = filesDirectory,
        };

        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, builder) =>
            {
                builder.SetBasePath(serverProjectDirectory);
                builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
                builder.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false);
                builder.AddInMemoryCollection(overrides);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseContentRoot(serverProjectDirectory);
                webBuilder.UseEnvironment("Development");
                webBuilder.UseUrls(baseUrl);
                webBuilder.UseStartup<SkillChat.Server.Startup>();
            })
            .Build();

        await host.StartAsync();
        await WaitForServerAsync(baseUrl);

        var documentStore = host.Services.GetRequiredService<IDocumentStore>();
        return new ServerTestHost(host, rootDirectory, baseUrl, filesDirectory, documentStore);
    }

    public JsonServiceClient CreateClient(string? bearerToken = null)
    {
        return new JsonServiceClient(BaseUrl)
        {
            BearerToken = bearerToken
        };
    }

    public async Task<User> CreateUserAsync(
        string? login = null,
        string? password = "Password123!",
        string? displayName = null,
        string? aboutMe = null)
    {
        using var session = DocumentStore.OpenAsyncSession();

        var normalizedLogin = (login ?? $"user-{Guid.NewGuid():N}@test.local").ToLowerInvariant();
        var user = new User
        {
            Id = $"User/{Guid.NewGuid():N}",
            Login = normalizedLogin,
            DisplayName = displayName,
            AboutMe = aboutMe,
            RegisteredTime = DateTimeOffset.UtcNow,
        };

        await session.StoreAsync(user);

        if (password is not null)
        {
            var salt = Hashing.CreateSalt();
            await session.StoreAsync(new UserSecret
            {
                Id = $"{user.Id}/secret",
                Salt = salt,
                Password = Hashing.CreateHashPassword(password, salt),
            });
        }

        await session.SaveChangesAsync();
        return user;
    }

    public async Task<Chat> CreateChatAsync(
        string? chatId = null,
        string? chatName = null,
        params string[] memberIds)
    {
        using var session = DocumentStore.OpenAsyncSession();

        var chat = new Chat
        {
            Id = chatId ?? $"Chat/{Guid.NewGuid():N}",
            ChatName = chatName ?? $"Chat-{Guid.NewGuid():N}",
            ChatType = ChatType.Public,
            OwnerId = memberIds.FirstOrDefault() ?? string.Empty,
            Members = memberIds
                .Distinct()
                .Select(id => new ChatMember
                {
                    UserId = id,
                    UserRole = ChatMemberRole.Participient,
                })
                .ToList(),
        };

        await session.StoreAsync(chat);
        await session.SaveChangesAsync();
        return chat;
    }

    public async Task<Attachment> CreateAttachmentAsync(
        string userId,
        string? fileName = null,
        byte[]? content = null)
    {
        content ??= "attachment-body"u8.ToArray();
        var attachmentId = $"attachment/{Guid.NewGuid():N}";
        var storagePath = Path.Combine(FilesPath, attachmentId["attachment/".Length..]);
        Directory.CreateDirectory(Path.GetDirectoryName(storagePath)!);
        await File.WriteAllBytesAsync(storagePath, content);

        using var session = DocumentStore.OpenAsyncSession();
        var attachment = new Attachment
        {
            Id = attachmentId,
            FileName = fileName ?? $"file-{Guid.NewGuid():N}.txt",
            SenderId = userId,
            Hash = Convert.ToHexString(content),
            Size = content.Length,
            UploadDateTime = DateTimeOffset.UtcNow,
        };

        await session.StoreAsync(attachment);
        await session.SaveChangesAsync();
        return attachment;
    }

    public async Task<Message> CreateMessageAsync(
        string chatId,
        string userId,
        string text,
        DateTimeOffset? postTime = null,
        IEnumerable<string>? attachmentIds = null,
        string? quotedMessageId = null,
        IEnumerable<string>? hiddenForUsers = null)
    {
        using var session = DocumentStore.OpenAsyncSession();
        var message = new Message
        {
            Id = $"Message/{Guid.NewGuid():N}",
            ChatId = chatId,
            UserId = userId,
            Text = text,
            PostTime = postTime ?? DateTimeOffset.UtcNow,
            Attachments = attachmentIds?.ToList(),
            IdQuotedMessage = quotedMessageId,
            HideForUsers = hiddenForUsers?.ToList(),
        };

        await session.StoreAsync(message);
        await session.SaveChangesAsync();
        return message;
    }

    public async Task<T?> LoadAsync<T>(string id)
    {
        using var session = DocumentStore.OpenAsyncSession();
        return await session.LoadAsync<T>(id);
    }

    public async Task<User?> FindUserByLoginAsync(string login)
    {
        using var session = DocumentStore.OpenAsyncSession();
        return await session.Query<User>()
            .Customize(x => x.WaitForNonStaleResults())
            .FirstOrDefaultAsync(user => user.Login == login.ToLowerInvariant());
    }

    public async Task<Chat?> FindChatByNameAsync(string chatName)
    {
        using var session = DocumentStore.OpenAsyncSession();
        return await session.Query<Chat>()
            .Customize(x => x.WaitForNonStaleResults())
            .FirstOrDefaultAsync(chat => chat.ChatName == chatName);
    }

    public async Task<HubConnectionContext> ConnectHubAsync(
        User user,
        string? token = null,
        string operatingSystem = "Windows Test",
        string ipAddress = "127.0.0.1",
        string clientName = "SkillChat Tests")
    {
        var connection = new HubConnectionBuilder()
            .WithUrl($"{BaseUrl}/chathub")
            .Build();
        var hub = connection.CreateHub<IChatHub>();
        var logOnSource = new TaskCompletionSource<LogOn>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.Subscribe<LogOn>(message => logOnSource.TrySetResult(message));

        await connection.StartAsync();
        await hub.Login(token ?? CreateAccessToken(user), operatingSystem, ipAddress, clientName);

        var logOn = await logOnSource.Task.WaitAsync(TimeSpan.FromSeconds(10));
        return new HubConnectionContext(connection, hub, logOn);
    }

    public string CreateAccessToken(User user, TimeSpan? expireIn = null, string? sessionId = null)
    {
        return CreateToken(user, expireIn ?? TimeSpan.FromMinutes(30), sessionId, permissions: null);
    }

    public string CreateRefreshToken(User user, TimeSpan? expireIn = null, string? sessionId = null)
    {
        return CreateToken(user, expireIn ?? TimeSpan.FromDays(30), sessionId, permissions: new[] { "refresh" });
    }

    private string CreateToken(User user, TimeSpan expireIn, string? sessionId, IEnumerable<string>? permissions)
    {
        var jwtProvider = (JwtAuthProvider)AuthenticateService.GetAuthProvider("jwt");
        var payload = JwtAuthProvider.CreateJwtPayload(
            new AuthUserSession
            {
                UserAuthId = user.Id,
                DisplayName = user.Login,
                Permissions = permissions?.ToList(),
                CreatedAt = DateTime.UtcNow,
            },
            issuer: jwtProvider.Issuer,
            expireIn: expireIn);

        payload["useragent"] = "SkillChat.Tests";
        payload["session"] = sessionId ?? Guid.NewGuid().ToString("N");

        return JwtAuthProvider.CreateEncryptedJweToken(payload, jwtProvider.PublicKey!.Value);
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();

        try
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
        catch
        {
        }
    }

    private static async Task WaitForServerAsync(string baseUrl)
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };

        for (var attempt = 0; attempt < 50; attempt++)
        {
            try
            {
                var response = await httpClient.GetAsync("/metadata");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
            }

            await Task.Delay(200);
        }

        throw new TimeoutException("The integration test host did not start in time.");
    }
}

internal sealed record HubConnectionContext(
    HubConnection Connection,
    IChatHub Hub,
    LogOn LogOn) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }
}
