using System;

namespace SkillChat.Client.ViewModel.Models
{
    public class UserMessageStatusModel
    {
        public string Id { get; set; }
        public string  UserId { get; set; }
        public string  ChatId { get; set; }
        public string LastReceivedMessageId { get; set; } = string.Empty;
        public DateTimeOffset LastReceivedMessageDate { get; set; } = DateTimeOffset.MinValue;
        public string LastReadedMessageId { get; set; } = string.Empty;
        public DateTimeOffset LastReadedMessageDate { get; set; } = DateTimeOffset.MinValue;
    }
}