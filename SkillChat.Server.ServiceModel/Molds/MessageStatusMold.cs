using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkillChat.Server.ServiceModel.Molds
{
    public class MessageStatusMold
    {
        public DateTimeOffset LastReceivedMessageDate { get; set; }
        public string LastReceivedMessageId { get; set; }
        public DateTimeOffset LastReadMessageDate { get; set; }
        public string LastReadMessageId { get; set; }
    }
}
