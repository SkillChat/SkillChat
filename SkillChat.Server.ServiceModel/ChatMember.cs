using System;
using System.Collections.Generic;
using System.Net;
using ServiceStack;
using SkillChat.Server.ServiceModel.Molds.Chats;

namespace SkillChat.Server.ServiceModel
{
    [Api("ChatMember")]
    [ApiResponse(HttpStatusCode.BadRequest, "Неверно составлен запрос", ResponseType = typeof(void))]
    [Route("/setchatmember", "POST", Summary = "Изменение начала истории сообщений", Notes = "Подгрузка сообщений с паджинацией")]
    public class SetChatMember : IReturn<ChatMemberMold>
    {
        [ApiMember(IsRequired = true, Description = "Начало переписки")]
        public DateTimeOffset? MessagesHistoryDateBegin { get; set; }

    }
}
