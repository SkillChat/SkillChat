using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Text;
using SignalR.EasyUse.Server;
using SkillChat.Interface;

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
            Log.Information($"User {Context.Items["login"]} send message in main chat");
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
                Log.Information($"Connected {Context.Items["login"]}({Context.Items["uid"]}) with session {Context.Items["session"]}");
            }
            catch (Exception e)
            {
                Log.Warning($"Bad token from connection {Context.ConnectionId}");
            }
        }
    }
}
