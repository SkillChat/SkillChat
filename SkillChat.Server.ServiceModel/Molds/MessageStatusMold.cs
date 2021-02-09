using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SkillChat.Server.ServiceModel.Molds
{
    [Description("Модель статуса сообщения")]
    public class MessageStatusMold
    {
        [Description("Дата прочтения")]
        public DateTimeOffset ReadDate { get; set; }
        
        [Description("Дата получения")]
        public DateTimeOffset ReceivedDate { get; set; }
        
        [Description("ID пользователя")]
        public string UserId { get; set; }

        [Description("ID сообщения")]
        public string MessageId { get; set; }
    }

    [Description("Список статусов сообщений")]
    public class MessageStatusCollectionMold
    {
        [Description("Список статусов сообщений")]
        public List<MessageStatusMold> Statuses { get; set; }
    }
}
