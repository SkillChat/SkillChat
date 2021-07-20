using System.Collections.Generic;
using System.ComponentModel;

namespace SkillChat.Server.ServiceModel.Molds
{
    [Description("Страница сообщений")]
    public class MessagePage
    {
        [Description("Cписок сообщений")]
        public List<MessageMold> Messages { get;set; }

        [Description("Последний статус сообщений в чате (нужно отправителям)")]
        public MessageStatusMold ChatMessageStatus { get; set; }
        [Description("Последний статус сообщений пользователя (нужно получателям)")]
        public MessageStatusMold MemberMessageStatus { get; set; }
    }
}