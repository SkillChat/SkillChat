using System;

namespace SkillChat.Server.Domain
{
    public class Message
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Text { get;set; }
        public DateTimeOffset PostTime { get; set; }
        public string ChatId { get; set; }
    }
}