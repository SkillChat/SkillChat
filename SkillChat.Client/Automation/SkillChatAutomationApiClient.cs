using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SkillChat.Client.ViewModel.Services;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.ServiceModel.Molds.Attachment;
using SkillChat.Server.ServiceModel.Molds.Chats;

namespace SkillChat.Client.Automation;

internal sealed class SkillChatAutomationApiClient : ISkillChatApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SkillChatAutomationState _state;

    public SkillChatAutomationApiClient(SkillChatAutomationState state)
    {
        _state = state;
        BearerToken = state.Tokens.AccessToken;
    }

    public string? BearerToken { get; set; }

    public Task<TokenResult> AuthenticateAsync(AuthViaPassword request) =>
        Task.FromResult(Clone(_state.Tokens));

    public Task<TokenResult> RefreshTokenAsync(PostRefreshToken request)
    {
        _state.Tokens = new TokenResult
        {
            AccessToken = $"{_state.Tokens.AccessToken}-refreshed",
            RefreshToken = $"{_state.Tokens.RefreshToken}-refreshed"
        };

        BearerToken = _state.Tokens.AccessToken;
        return Task.FromResult(Clone(_state.Tokens));
    }

    public Task<TokenResult> RegisterAsync(RegisterNewUser request)
    {
        _state.CurrentUser.Login = request.Login ?? string.Empty;
        _state.CurrentUser.UserName = request.UserName ?? string.Empty;
        _state.Profile.Login = request.Login ?? string.Empty;
        _state.Profile.DisplayName = request.UserName ?? string.Empty;
        _state.Profile.AboutMe ??= string.Empty;
        return Task.FromResult(Clone(_state.Tokens));
    }

    public Task<ChatPage> GetChatsAsync(GetChatsList request) =>
        Task.FromResult(new ChatPage
        {
            Chats = new List<ChatMold> { Clone(_state.ActiveChat) }
        });

    public Task<MessagePage> GetMessagesAsync(GetMessages request)
    {
        IEnumerable<MessageMold> messages = _state.Messages;

        if (!string.IsNullOrWhiteSpace(request.ChatId))
        {
            messages = messages.Where(message => string.Equals(message.ChatId, request.ChatId, StringComparison.Ordinal));
        }

        if (request.BeforePostTime.HasValue)
        {
            messages = messages.Where(message => message.PostTime < request.BeforePostTime.Value);
        }

        var pageSize = request.PageSize ?? 50;
        var page = messages
            .OrderByDescending(message => message.PostTime)
            .Take(pageSize + 1)
            .ToList();
        var hasMoreBefore = page.Count > pageSize;
        page = page.Take(pageSize).ToList();

        return Task.FromResult(new MessagePage
        {
            Messages = Clone(page),
            FirstUnreadMessageId = request.BeforePostTime.HasValue ? null : _state.FirstUnreadMessageId,
            HasMoreBefore = hasMoreBefore
        });
    }

    public Task<UserChatSettings> GetMySettingsAsync(GetMySettings request) =>
        Task.FromResult(Clone(_state.Settings));

    public Task<UserChatSettings> SaveSettingsAsync(SetSettings request)
    {
        _state.Settings.SendingMessageByEnterKey = request.SendingMessageByEnterKey;
        return Task.FromResult(Clone(_state.Settings));
    }

    public Task<UserProfileMold> GetProfileAsync(GetProfile request) =>
        Task.FromResult(Clone(_state.Profile));

    public Task<UserProfileMold> SaveProfileAsync(SetProfile request)
    {
        _state.Profile.DisplayName = request.DisplayName;
        _state.Profile.AboutMe = request.AboutMe;
        return Task.FromResult(Clone(_state.Profile));
    }

    public Task<LoginHistory> GetLoginAuditAsync(GetLoginAudit request) =>
        Task.FromResult(Clone(_state.LoginAudit));

    public Task<Stream> GetAttachmentAsync(GetAttachment request)
    {
        if (_state.AttachmentFilePaths.TryGetValue(request.Id, out var filePath) && File.Exists(filePath))
        {
            return Task.FromResult<Stream>(File.OpenRead(filePath));
        }

        throw new FileNotFoundException($"Attachment with id '{request.Id}' was not found in automation state.");
    }

    public AttachmentMold UploadAttachment(Stream fileStream, string fileName, SetAttachment request)
    {
        var attachmentId = Guid.NewGuid().ToString("N");

        return new AttachmentMold
        {
            Id = attachmentId,
            SenderId = _state.CurrentUser.Id,
            FileName = fileName,
            UploadDateTime = DateTimeOffset.UtcNow,
            Hash = attachmentId,
            Size = fileStream.Length
        };
    }

    private static T Clone<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, SerializerOptions);
        return JsonSerializer.Deserialize<T>(json, SerializerOptions)
            ?? throw new InvalidOperationException($"Failed to clone automation value of type {typeof(T).FullName}.");
    }
}
