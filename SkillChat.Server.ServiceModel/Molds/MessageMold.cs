using System;
using System.ComponentModel;

namespace SkillChat.Server.ServiceModel.Molds
{
    [Description("Сообщение со страницы сообщений")]
    public class MessageMold
    {
        [Description("Идентификатор сообщения")]
        public string Id { get; set; }
        [Description("Логин пользователя")]
        public string UserLogin { get; set; }
        [Description("Текст сообщения")]
        public string Text { get; set; }
        [Description("Время публикации")]
        public DateTimeOffset PostTime { get; set; }
        [Description("Идентификатор чата")]
        public string ChatId{get;set;}
    }
}