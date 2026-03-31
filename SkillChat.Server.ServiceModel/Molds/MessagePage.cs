using System.Collections.Generic;
using System.ComponentModel;

namespace SkillChat.Server.ServiceModel.Molds
{
    [Description("Страница сообщений")]
    public class MessagePage
    {
        [Description("Cписок сообщений")]
        public List<MessageMold> Messages { get;set; }

        [Description("Идентификатор первого непрочитанного сообщения во всей видимой истории")]
        public string FirstUnreadMessageId { get; set; }

        [Description("Есть ли в истории более старые сообщения, чем текущая страница")]
        public bool HasMoreBefore { get; set; }
    }
}
