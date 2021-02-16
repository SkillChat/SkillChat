using System;
using System.ComponentModel;

namespace SkillChat.Server.ServiceModel.Molds.Chats
{
    [Description("Участник чата")]
    public class ChatMemberMold
    {
        [Description("Идентификатор пользователя")]
        public string UserId { get; set; }
        [Description("Роль")]
        public ChatMemberRoleMold UserRole { get; set; }
        [Description("Начало по времени истории сообщений")]
        public DateTimeOffset MessagesHistoryDateBegin { get; set; }
    }

    [Description("Роль участника чата")]
    public enum ChatMemberRoleMold
    {
        [Description("Модератор")]
        Moderator = 1,
        [Description("Участник")]
        Participient,
        [Description("Только для чтения")]
        Viewer
    }
}
