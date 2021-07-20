using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SignalR.EasyUse.Interface;

namespace SkillChat.Interface
{
    public class ReceiveMessageStatus : IClientMethod
    {
        public DateTimeOffset LastReceivedMessageDate { get; set; }
        public string LastReceivedMessageId { get; set; }
        public DateTimeOffset LastReadMessageDate { get; set; }
        public string LastReadMessageId { get; set; }
        public string ChatId { get; set; }
    }
}
