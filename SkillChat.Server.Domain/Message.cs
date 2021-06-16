using System;
using System.Collections.Generic;

namespace SkillChat.Server.Domain
{
    public class Message
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Text { get; set; }
        public DateTimeOffset PostTime { get; set; }
        public string ChatId { get; set; }
        public List<string> Attachments { get; set; }
        public bool IsReceived { get; set; }
        public bool IsRead { get; set; }
    }
}