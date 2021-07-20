using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkillChat.Server.Domain.MessStatus
{
    public class MessageStatus
    {
        public DateTimeOffset LastReceivedMessageDate { get; set; }
        public string LastReceivedMessageId { get; set; }
        public DateTimeOffset LastReadMessageDate { get; set; }
        public string LastReadMessageId { get; set; }

        public MessageStatus()
        {
            LastReceivedMessageDate = LastReadMessageDate = DateTimeOffset.MinValue;
            LastReceivedMessageId = LastReadMessageId = "";
        }

        public MessageStatus(Message message)
        {
            LastReceivedMessageDate = LastReadMessageDate = message.PostTime;
            LastReceivedMessageId = LastReadMessageId = message.Id;
        }
        public MessageStatus(Message receivedMessage, Message readMessage)
        {
            LastReceivedMessageDate = receivedMessage.PostTime;
            LastReadMessageDate = readMessage.PostTime;
            LastReceivedMessageId = receivedMessage.Id;
            LastReadMessageId = readMessage.Id;
        }
        public bool Update(MessageStatus newStatus)
        {
            bool result = false;
            if (LastReadMessageDate <= newStatus.LastReadMessageDate)
            {
                LastReadMessageDate = newStatus.LastReadMessageDate;
                LastReadMessageId = newStatus.LastReadMessageId;
                result = true;
            }
            if (LastReceivedMessageDate <= newStatus.LastReadMessageDate)
            {
                LastReceivedMessageDate = newStatus.LastReadMessageDate;
                LastReceivedMessageId = newStatus.LastReadMessageId;
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
