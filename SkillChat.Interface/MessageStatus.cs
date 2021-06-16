using System;
using SignalR.EasyUse.Interface;

namespace SkillChat.Interface
{
    /// <summary>Статус сообщения, отправляется клиентом на сервер</summary>
    public class MessageStatus : IClientMethod
    {
        public string MessageId { get; set; }
        public DateTimeOffset? ReadDate { get; set; }
        public DateTimeOffset? ReceivedDate { get; set; }
    }
}