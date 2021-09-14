using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkillChat.Server.Domain
{
    /// <summary>id = userId + chatId</summary>
    public class UserMessageStatus
    {
        public string Id { get; set; }
        public string  UserId { get; set; }
        public string  ChatId { get; set; }
        public string LastReceivedMessageId { get; set; } = string.Empty;
        public DateTimeOffset LastReceivedMessageDate { get; set; } = DateTimeOffset.MinValue;
        public string LastReadedMessageId { get; set; } = string.Empty;
        public DateTimeOffset LastReadedMessageDate { get; set; } = DateTimeOffset.MinValue;

        public UserMessageStatus(string userId, string chatId)
        {
            this.Id = userId + chatId;
            UserId = userId;
            ChatId = chatId;
        }
        
        public static bool operator == (UserMessageStatus status1, UserMessageStatus status2)
        {
            return status1?.LastReceivedMessageDate == status2?.LastReceivedMessageDate
                   && status1?.LastReadedMessageDate == status2?.LastReadedMessageDate
                   && status1?.Id == status2?.Id
                   && status1?.UserId == status2?.UserId
                   && status1?.ChatId == status2?.ChatId;
        }
        public static bool operator !=(UserMessageStatus status1, UserMessageStatus status2)
        {
            return !(status1?.LastReceivedMessageDate == status2?.LastReceivedMessageDate
                   && status1?.LastReadedMessageDate == status2?.LastReadedMessageDate
                   && status1?.Id == status2?.Id
                   && status1?.UserId == status2?.UserId
                   && status1?.ChatId == status2?.ChatId);
        }
    }
}
