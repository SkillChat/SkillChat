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

        /// <summary>Сколько раз прочитали</summary>
        [Description("Сколько раз прочитали")]
        public long? ReadCount { get; set; }
        /// <summary>Сколько раз получили</summary>
        [Description("Сколько раз получили")]
        public long? ReceivedCount { get; set; }
        /// <summary>Прочтено ли?</summary>
        [Description("Прочтено ли?")]
        public bool Read { get; set; }
        /// <summary>Получено ли?</summary>
        [Description("Получено ли?")]
        public bool Received { get; set; }
    }
}