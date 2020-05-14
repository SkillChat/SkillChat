using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using ServiceStack;
using SkillChat.Server.ServiceModel.Molds;

namespace SkillChat.Server.ServiceModel
{
    [Api("Chat")]
    [ApiResponse(HttpStatusCode.BadRequest, "Неверно составлен запрос", ResponseType = typeof(void))]
    [Route("/message", "GET", Summary = "Получение истории сообщений", Notes = "Подгрузка сообщений с паджинацией")]
    public class GetMessages : IReturn<MessagePage>
    {
        [ApiMember(IsRequired = false, Description = "Идентиикатор сообщения после которрого получать")]
        public DateTimeOffset? BeforePostTime { get; set; }
        
        [ApiMember(IsRequired = false, Description = "Идентиикатор сообщения после которрого получать")]
        public int? PageSize { get;set; }
    }
}
