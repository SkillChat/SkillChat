﻿using System.Collections.Generic;
using System.ComponentModel;

namespace SkillChat.Server.ServiceModel.Molds
{
    [Description("Страница сообщений")]
    public class MessagePage
    {
        [Description("Cписок сообщений")]
        public List<MessageMold> Messages { get;set; }
        public UserMessageStatusMold MessageStatus { get; set; }
    }
}