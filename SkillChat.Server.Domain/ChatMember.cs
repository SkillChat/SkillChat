using System;
using System.Collections.Generic;
using System.Text;
using SkillChat.Server.Domain;
using SkillChat.Server.Domain.MessStatus;

namespace SkillChat.Server.Domain
{
    public class ChatMember
    {
        public string UserId { get; set; }
        public ChatMemberRole UserRole { get; set; }
        /// <summary>
        /// Последнее полученное/прочитанное сообщение пользователя
        /// </summary>
        public MessageStatus MessageStatus { get; set; }
    }

    public enum ChatMemberRole
    {
        Moderator = 1,
        Participient,
        Viewer
    }
}
