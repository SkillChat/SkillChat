using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkillChat.Server.Domain.MessStatus
{
    /// <summary>
    /// For Chat status Id = ChatId;
    /// For ChatMember status Id = ChatId+UserId
    /// </summary>
    public class MessageStatus
    {
        public string Id { get; set; }
        public DateTimeOffset LastReceivedMessageDate { get; set; }
        public string LastReceivedMessageId { get; set; }
        public DateTimeOffset LastReadedMessageDate { get; set; }
        public string LastReadedMessageId { get; set; }

        public MessageStatus()
        {
            LastReceivedMessageDate = LastReadedMessageDate = DateTimeOffset.MinValue;
            LastReceivedMessageId = LastReadedMessageId = "";
        }
        
        public bool Update(MessageStatus newStatus)
        {
            bool result = false;
            if (LastReadedMessageDate <= newStatus.LastReadedMessageDate)
            {
                LastReadedMessageDate = newStatus.LastReadedMessageDate;
                LastReadedMessageId = newStatus.LastReadedMessageId;
                result = true;
            }
            if (LastReceivedMessageDate <= newStatus.LastReadedMessageDate)
            {
                LastReceivedMessageDate = newStatus.LastReadedMessageDate;
                LastReceivedMessageId = newStatus.LastReadedMessageId;
                result = true;
            }
            if (LastReceivedMessageDate <= newStatus.LastReceivedMessageDate)
            {
                LastReceivedMessageDate = newStatus.LastReceivedMessageDate;
                LastReceivedMessageId = newStatus.LastReceivedMessageId;
                result = true;
            }
            return result;
        }
    }
}
