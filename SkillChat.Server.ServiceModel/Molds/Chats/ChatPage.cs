using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkillChat.Server.ServiceModel.Molds.Chats
{
    [Description("Страница список чатов")]
    public class ChatPage
    {
        [Description("Cписок сообщений")]
        public List<ChatMold> Chats { get; set; }
    }
}
