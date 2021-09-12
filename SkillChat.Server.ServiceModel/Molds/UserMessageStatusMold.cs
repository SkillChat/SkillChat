using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkillChat.Server.ServiceModel.Molds
{
    public class UserMessageStatusMold
    {
        public string LastReceivedMessageId { get; set; } = string.Empty;
        public DateTimeOffset LastReceivedMessageDate { get; set; } = DateTimeOffset.MinValue;
        public string LastReadedMessageId { get; set; } = string.Empty;
        public DateTimeOffset LastReadedMessageDate { get; set; } = DateTimeOffset.MinValue;
    }
}
