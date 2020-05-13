using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Text;
using SignalR.EasyUse.Server;
using SkillChat.Interface;
using SkillChat.Server.Domain;

namespace SkillChat.Server.Hubs
{
    public class ChatHub : Hub, IChatHub
    {
        private string _loginedGroup = "Logined";
        
        public async Task SendMessage(string message)
        {
            await Clients.Group(_loginedGroup).SendAsync(new ReceiveMessage
            {
                User = Context.Items["login"] as string,
                Message = message,
            });
        }

        public async Task Login(string token)
        {
            var jwtAuthProviderReader = (JwtAuthProviderReader)AuthenticateService.GetAuthProvider("jwt");

            JsonObject jwtPayload = null;
            try
            {
                jwtPayload = jwtAuthProviderReader.GetVerifiedJwtPayload(new BasicHttpRequest(), token.Split('.'));
                await Groups.AddToGroupAsync(this.Context.ConnectionId, _loginedGroup);
                Context.Items["login"] = jwtPayload["name"];
                Context.Items["uid"] = jwtPayload["sub"];
                Context.Items["session"] = jwtPayload["session"];
            }
            catch (Exception e)
            {
                throw new HttpError(HttpStatusCode.Unauthorized, "Токен неверный");
            }

        }
    }
}
