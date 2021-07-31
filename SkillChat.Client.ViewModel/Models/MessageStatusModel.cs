using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkillChat.Client.ViewModel.Models
{
    public class MessageStatusModel
    {
        public DateTimeOffset LastReceivedMessageDate { get; set; } = DateTimeOffset.MinValue;
        public string LastReceivedMessageId { get; set; } = "";
        public DateTimeOffset LastReadedMessageDate { get; set; } = DateTimeOffset.MinValue;
        public string LastReadedMessageId { get; set; } = "";
        public string ChatId { get; set; } = "";

        public bool Update(MessageStatusModel newStatus)
        {
            bool result = false;
            if ((LastReadedMessageDate < newStatus.LastReadedMessageDate || LastReadedMessageDate == DateTimeOffset.MinValue)
                && (ChatId == newStatus.ChatId || string.IsNullOrWhiteSpace(ChatId)))
            {
                LastReadedMessageDate = newStatus.LastReadedMessageDate;
                LastReadedMessageId = newStatus.LastReadedMessageId;
                result = true;
            }

            if ((LastReceivedMessageDate < newStatus.LastReadedMessageDate || LastReadedMessageDate == DateTimeOffset.MinValue)
                && (ChatId == newStatus.ChatId || string.IsNullOrWhiteSpace(ChatId)))
            {
                LastReceivedMessageDate = newStatus.LastReadedMessageDate;
                LastReceivedMessageId = newStatus.LastReadedMessageId;
                result = true;
            }else if (LastReceivedMessageDate < newStatus.LastReceivedMessageDate 
                      && (ChatId == newStatus.ChatId || string.IsNullOrWhiteSpace(ChatId)))
            {
                LastReceivedMessageDate = newStatus.LastReceivedMessageDate;
                LastReceivedMessageId = newStatus.LastReceivedMessageId;
                result = true;
            }
            return result;
        }
    }
}
