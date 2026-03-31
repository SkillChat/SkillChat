using System;
using System.Collections.Generic;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.ServiceModel.Molds.Attachment;
using SkillChat.Server.ServiceModel.Molds.Chats;

namespace SkillChat.Client.Automation;

public sealed class SkillChatAutomationState
{
    public SkillChatAutomationCurrentUser CurrentUser { get; set; } = new();
    public TokenResult Tokens { get; set; } = new();
    public UserProfileMold Profile { get; set; } = new();
    public UserChatSettings Settings { get; set; } = new();
    public LoginHistory LoginAudit { get; set; } = new() { History = new List<UserLoginAudit>() };
    public ChatMold ActiveChat { get; set; } = new();
    public string MembersCaption { get; set; } = string.Empty;
    public List<MessageMold> Messages { get; set; } = new();
    public string FirstUnreadMessageId { get; set; } = string.Empty;
    public bool IsDarkTheme { get; set; } = true;
    public bool IsSidebarExpanded { get; set; } = true;
    public double WindowWidth { get; set; } = 900;
    public List<string> FileDialogSelection { get; set; } = new();
    public Dictionary<string, string> AttachmentFilePaths { get; set; } = new();
}

public sealed class SkillChatAutomationCurrentUser
{
    public string Id { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
