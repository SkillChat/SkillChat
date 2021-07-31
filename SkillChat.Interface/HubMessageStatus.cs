using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkillChat.Interface
{
    public class HubMessageStatus
    {
        public DateTimeOffset LastReceivedMessageDate { get; set; }
        public string LastReceivedMessageId { get; set; }
        public DateTimeOffset LastReadedMessageDate { get; set; }
        public string LastReadedMessageId { get; set; }
        public string ChatId { get; set; }
    }
}
