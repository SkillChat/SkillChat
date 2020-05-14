using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Raven.Client.Documents.Session;
using Serilog;
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
        public ChatHub(IAsyncDocumentSession ravenSession)
        {
            _ravenSession = ravenSession;
        }

        private string _loginedGroup = "Logined";

        private readonly IAsyncDocumentSession _ravenSession;
        
        public async Task SendMessage(string message)
        {
            var messageItem = new Message
            {
                UserId = Context.Items["uid"] as string,
                Text = message,
                PostTime = DateTimeOffset.UtcNow,
            };

            await _ravenSession.StoreAsync(messageItem);
            await _ravenSession.SaveChangesAsync();

            await Clients.Group(_loginedGroup).SendAsync(new ReceiveMessage
            {
                Id = messageItem.Id,
                UserLogin = Context.Items["login"] as string,
                Message = message,
                PostTime = messageItem.PostTime,
            });
            
            Log.Information($"User {Context.Items["login"]} send message in main chat");
        }

        public async Task Login(string token)
        {
            var jwtAuthProviderReader = (JwtAuthProviderReader)AuthenticateService.GetAuthProvider("jwt");

            try
            {
                var jwtPayload = jwtAuthProviderReader.GetVerifiedJwtPayload(new BasicHttpRequest(), token.Split('.'));
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
