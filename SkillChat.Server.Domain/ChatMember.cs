﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SkillChat.Server.Domain
{
    public class ChatMember
    {
        public DateTimeOffset MessagesHistoryDateBegin { get; set; }
        public string UserId { get; set; }
        public ChatMemberRole UserRole { get; set; }
    }

    public enum ChatMemberRole
    {
        Moderator = 1,
        Participient,
        Viewer
    }
}
