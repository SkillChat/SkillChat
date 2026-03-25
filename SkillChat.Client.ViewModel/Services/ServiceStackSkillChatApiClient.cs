using System.IO;
using System.Threading.Tasks;
using ServiceStack;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.ServiceModel.Molds.Attachment;
using SkillChat.Server.ServiceModel.Molds.Chats;

namespace SkillChat.Client.ViewModel.Services;

public sealed class ServiceStackSkillChatApiClient : ISkillChatApiClient
{
    private readonly IJsonServiceClient _client;

    public ServiceStackSkillChatApiClient(string hostUrl)
    {
        _client = new JsonServiceClient(hostUrl);
    }

    public string? BearerToken
    {
        get => _client.BearerToken;
        set => _client.BearerToken = value;
    }

    public Task<TokenResult> AuthenticateAsync(AuthViaPassword request) => _client.PostAsync(request);

    public Task<TokenResult> RefreshTokenAsync(PostRefreshToken request) => _client.PostAsync(request);

    public Task<TokenResult> RegisterAsync(RegisterNewUser request) => _client.PostAsync(request);

    public Task<ChatPage> GetChatsAsync(GetChatsList request) => _client.GetAsync(request);

    public Task<MessagePage> GetMessagesAsync(GetMessages request) => _client.GetAsync(request);

    public Task<UserChatSettings> GetMySettingsAsync(GetMySettings request) => _client.GetAsync(request);

    public Task<UserChatSettings> SaveSettingsAsync(SetSettings request) => _client.PostAsync(request);

    public Task<UserProfileMold> GetProfileAsync(GetProfile request) => _client.GetAsync(request);

    public Task<UserProfileMold> SaveProfileAsync(SetProfile request) => _client.PostAsync(request);

    public Task<LoginHistory> GetLoginAuditAsync(GetLoginAudit request) => _client.GetAsync(request);

    public Task<Stream> GetAttachmentAsync(GetAttachment request) => _client.GetAsync(request);

    public AttachmentMold UploadAttachment(Stream fileStream, string fileName, SetAttachment request) =>
        _client.PostFileWithRequest<AttachmentMold>(fileStream, fileName, request);
}
