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
        public DateTimeOffset LastReadMessageDate { get; set; } = DateTimeOffset.MinValue;
        public string LastReadMessageId { get; set; } = "";
        public string ChatId { get; set; } = "";

        public bool Update(MessageStatusModel newStatus)
        {
            bool result = false;
            if ((LastReadMessageDate < newStatus.LastReadMessageDate || LastReadMessageDate == DateTimeOffset.MinValue)
                && (ChatId == newStatus.ChatId || string.IsNullOrWhiteSpace(ChatId)))
            {
                LastReadMessageDate = newStatus.LastReadMessageDate;
                LastReadMessageId = newStatus.LastReadMessageId;
                result = true;
            }

            if ((LastReceivedMessageDate < newStatus.LastReadMessageDate || LastReadMessageDate == DateTimeOffset.MinValue)
                && (ChatId == newStatus.ChatId || string.IsNullOrWhiteSpace(ChatId)))
            {
                LastReceivedMessageDate = newStatus.LastReadMessageDate;
                LastReceivedMessageId = newStatus.LastReadMessageId;
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
