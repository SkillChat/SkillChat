#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using ServiceStack;
using SkillChat.Server.ServiceModel.Molds;

namespace SkillChat.Server.ServiceModel
{
    [Api("LoginAudit")]
    [ApiResponse(HttpStatusCode.BadRequest, "Неверно составлен запрос", ResponseType = typeof(void))]
    [Route("/loginAudit", "GET", Summary = "Получение истории входов", Notes = "Все факты входа пользователя")]
    public class GetLoginAudit : IReturn<LoginHistory> { }
}