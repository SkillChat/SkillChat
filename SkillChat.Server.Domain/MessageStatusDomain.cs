using System;
using System.Xml;

namespace SkillChat.Server.Domain
{
    /// <summary>Хранилище статусов сообщений</summary>
    public class MessageStatusDomain
    {
        public string MessageId { get; set; }
        public string UserId { get; set; }
        public DateTimeOffset ReadDate { get; set; }
        public DateTimeOffset ReceivedDate { get; set; }
    }
}