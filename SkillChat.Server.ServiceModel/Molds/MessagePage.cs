using System.Collections.Generic;
using System.ComponentModel;
using SkillChat.Server.ServiceModel.Molds.Status;

namespace SkillChat.Server.ServiceModel.Molds
{
    [Description("Страница сообщений")]
    public class MessagePage
    {
        [Description("Cписок сообщений")]
        public List<MessageMold> Messages { get;set; }
    }
}