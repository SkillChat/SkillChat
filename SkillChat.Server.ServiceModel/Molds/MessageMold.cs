using System;
using System.ComponentModel;

namespace SkillChat.Server.ServiceModel.Molds
{
    [Description("Страница сообщений")]
    public class MessageMold
    {
        public string Id { get; set; }
        public string UserLogin { get; set; }
        public string Text { get; set; }
        public DateTimeOffset PostTime { get; set; }
    }
}