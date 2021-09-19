using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using ServiceStack;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.ServiceModel.Molds.Chats;
using SkillChat.Server.ServiceModel.Molds.Status;

namespace SkillChat.Server.ServiceModel
{
    [Api("Chat")]
    [ApiResponse(HttpStatusCode.BadRequest, "Неверно составлен запрос", ResponseType = typeof(void))]
    [Route("/message", "GET", Summary = "Получение истории сообщений", Notes = "Подгрузка сообщений с паджинацией")]
    public class GetMessages : IReturn<MessagePage>
    {
        [ApiMember(IsRequired = true, Description = "Идентификатор чата, для которого будет выбираться сообщения")]
        public string ChatId { get; set; }

        [ApiMember(IsRequired = false, Description = "Идентиикатор сообщения после которрого получать")]
        public DateTimeOffset? BeforePostTime { get; set; }
        
        [ApiMember(IsRequired = false, Description = "Идентиикатор сообщения после которрого получать")]
        public int? PageSize { get;set; }
    }

    [Api("Chat")]
    [ApiResponse(HttpStatusCode.BadRequest, "Неверно составлен запрос", ResponseType = typeof(void))]
    [Route("/chats", "GET", Summary = "Получение списка чатов", Notes = "Получение списка чатов")]
    public class GetChatsList: IReturn<ChatPage>
    {}
    
    [Api("UserMessageStatus")]
    [ApiResponse(HttpStatusCode.BadRequest, "Неверно составлен запрос", ResponseType = typeof(void))]
    [Route("/userStatuses", "GET", Summary = "Получение списка статусов пользователя", Notes = "Получение списка статусов пользователя")]
    public class GetStatuses: IReturn<UserMessageStatusPage>
    {}
}
