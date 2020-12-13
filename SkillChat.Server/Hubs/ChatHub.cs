using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Raven.Client.Documents.Session;
using Serilog;
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
        public ChatHub(IAsyncDocumentSession ravenSession)
        {
            _ravenSession = ravenSession;
        }

        private string _loginedGroup = "Logined";

        private readonly IAsyncDocumentSession _ravenSession;
        
        public async Task SendMessage(string message, string chatId)
        {
            var messageItem = new Message
            {
                UserId = Context.Items["uid"] as string,
                Text = message,
                PostTime = DateTimeOffset.UtcNow,
                ChatId = chatId,
            };

            await _ravenSession.StoreAsync(messageItem);
            await _ravenSession.SaveChangesAsync();

            await Clients.Group(_loginedGroup).SendAsync(new ReceiveMessage
            {
                Id = messageItem.Id,
                UserLogin = Context.Items["login"] as string,
                Message = message,
                PostTime = messageItem.PostTime,
                ChatId = chatId,
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
                

                var logOn = new LogOn
                {
                    Id = jwtPayload["sub"],
                    UserLogin = jwtPayload["name"],
                };
                if (long.TryParse(jwtPayload["exp"], out long expire))
                {
                    logOn.ExpireTime = DateTimeOffset.FromUnixTimeSeconds(expire);
                    if (logOn.ExpireTime < DateTimeOffset.UtcNow)
                    {
                        throw new TokenException("Token is expired");
                    }
                }

                await Clients.Caller.SendAsync(logOn);
                Log.Information($"Connected {Context.Items["login"]}({Context.Items["uid"]}) with session {Context.Items["session"]}");
            }
            catch (Exception e)
            {
                await Clients.Caller.SendAsync(new LogOn
                {
                    Error = true
                });
                Log.Warning($"Bad token from connection {Context.ConnectionId}");
            }
        }
    }
}
