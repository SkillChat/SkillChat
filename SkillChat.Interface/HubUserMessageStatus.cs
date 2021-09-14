using System;

namespace SkillChat.Interface
{
    public class HubUserMessageStatus
    {
        public string Id { get; set; }
        public string LastReceivedMessageId { get; set; } = string.Empty;
        public DateTimeOffset LastReceivedMessageDate { get; set; } = DateTimeOffset.MinValue;
        public string LastReadedMessageId { get; set; } = string.Empty;
        public DateTimeOffset LastReadedMessageDate { get; set; } = DateTimeOffset.MinValue;
    }
}