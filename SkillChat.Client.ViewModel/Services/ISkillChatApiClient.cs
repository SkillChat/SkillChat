using System.IO;
using System.Threading.Tasks;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.ServiceModel.Molds.Attachment;
using SkillChat.Server.ServiceModel.Molds.Chats;

namespace SkillChat.Client.ViewModel.Services;

public interface ISkillChatApiClient
{
    string? BearerToken { get; set; }

    Task<TokenResult> AuthenticateAsync(AuthViaPassword request);
    Task<TokenResult> RefreshTokenAsync(PostRefreshToken request);
    Task<TokenResult> RegisterAsync(RegisterNewUser request);
    Task<ChatPage> GetChatsAsync(GetChatsList request);
    Task<MessagePage> GetMessagesAsync(GetMessages request);
    Task<UserChatSettings> GetMySettingsAsync(GetMySettings request);
    Task<UserChatSettings> SaveSettingsAsync(SetSettings request);
    Task<UserProfileMold> GetProfileAsync(GetProfile request);
    Task<UserProfileMold> SaveProfileAsync(SetProfile request);
    Task<LoginHistory> GetLoginAuditAsync(GetLoginAudit request);
    Task<Stream> GetAttachmentAsync(GetAttachment request);
    AttachmentMold UploadAttachment(Stream fileStream, string fileName, SetAttachment request);
}
