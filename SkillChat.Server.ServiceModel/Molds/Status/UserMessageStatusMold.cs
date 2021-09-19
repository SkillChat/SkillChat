using System;

namespace SkillChat.Server.ServiceModel.Molds.Status
{
    public class UserMessageStatusMold
    {
        public string Id { get; set; }
        public string ChatId { get; set; }
        public string LastReceivedMessageId { get; set; } = string.Empty;
        public DateTimeOffset LastReceivedMessageDate { get; set; } = DateTimeOffset.MinValue;
        public string LastReadedMessageId { get; set; } = string.Empty;
        public DateTimeOffset LastReadedMessageDate { get; set; } = DateTimeOffset.MinValue;
    }
}
