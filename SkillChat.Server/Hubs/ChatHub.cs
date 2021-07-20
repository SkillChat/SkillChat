using Microsoft.AspNetCore.SignalR;
using Raven.Client.Documents.Session;
using Serilog;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Host;
using SignalR.EasyUse.Server;
using SkillChat.Interface;
using SkillChat.Server.Domain;
using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using SkillChat.Server.Domain.MessStatus;

namespace SkillChat.Server.Hubs
{
    public class ChatHub : Hub, IChatHub
    {
        private IMapper mapper;
        public ChatHub(IAsyncDocumentSession ravenSession, IMapper mapper)
        {
            _ravenSession = ravenSession;
            this.mapper = mapper;
        }

        private string _loginedGroup = "Logined";

        private readonly IAsyncDocumentSession _ravenSession;

        public async Task UpdateMyDisplayName(string userDispalyName)
        {
            if (Context.Items["nickname"] as string != userDispalyName)
            {
                await Clients.Group(_loginedGroup).SendAsync(new UpdateUserDisplayName
                {
                    Id = Context.Items["uid"] as string,
                    DisplayName = userDispalyName
                });

                Context.Items["nickname"] = userDispalyName;
                Log.Information(
                    $"User Id:{Context.Items["uid"] as string} change display user name to {userDispalyName}");
            }
        }

        public async Task SendMessage(HubMessage hubMessage)
        {
            var messageItem = new Message
            {
                UserId = Context.Items["uid"] as string,
                Text = hubMessage.Message,
                PostTime = DateTimeOffset.UtcNow,
                ChatId = hubMessage.ChatId,
                Attachments = hubMessage.Attachments?.Select(s => s.Id).ToList()
            };

            await _ravenSession.StoreAsync(messageItem);
            await _ravenSession.SaveChangesAsync();

            await Clients.Group(_loginedGroup).SendAsync(new ReceiveMessage
            {
                Id = messageItem.Id,
                UserLogin = Context.Items["login"] as string,
                UserNickname = Context.Items["nickname"] as string,
                Message = hubMessage.Message,
                PostTime = messageItem.PostTime,
                ChatId = hubMessage.ChatId,
                UserId = messageItem.UserId,
                Attachments = hubMessage.Attachments
            });

            Log.Information($"User {Context.Items["nickname"]}({Context.Items["login"]}) send message in main chat");
        }

        public async Task Login(string token, string operatingSystem, string ipAddress, string nameVersionClient)
        {
            var jwtAuthProviderReader = (JwtAuthProviderReader)AuthenticateService.GetAuthProvider("jwt");

            try
            {
                var jwtPayload = jwtAuthProviderReader.GetVerifiedJwtPayload(new BasicHttpRequest(), token.Split('.'));
                await Groups.AddToGroupAsync(this.Context.ConnectionId, _loginedGroup);
                Context.Items["login"] = jwtPayload["name"];
                Context.Items["uid"] = jwtPayload["sub"];
                Context.Items["session"] = jwtPayload["session"];

                var user = await _ravenSession.LoadAsync<User>(jwtPayload["sub"]);
                if (user != null)
                {
                    Context.Items["nickname"] = user.DisplayName;
                }

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
                var userLoginAudit = await _ravenSession.LoadAsync<LoginAudit>(jwtPayload["sub"] + "/LoginAudit");
                if (userLoginAudit != null)
                {
                    if (jwtPayload["session"] != userLoginAudit.SessionId)
                    {
                        userLoginAudit.NameVersionClient = nameVersionClient;
                        userLoginAudit.OperatingSystem = operatingSystem;
                        userLoginAudit.IpAddress = ipAddress;
                        userLoginAudit.DateOfEntry = DateTime.Now;
                        userLoginAudit.SessionId = jwtPayload["session"];

                        await _ravenSession.StoreAsync(userLoginAudit);
                        await _ravenSession.SaveChangesAsync();
                    }
                }
                else
                {
                    userLoginAudit = new LoginAudit
                    {
                        Id = jwtPayload["sub"] + "/LoginAudit",
                        OperatingSystem = operatingSystem,
                        DateOfEntry = DateTime.Now,
                        IpAddress = ipAddress,
                        NameVersionClient = nameVersionClient,
                        SessionId = jwtPayload["session"]
                    };
                    await _ravenSession.StoreAsync(userLoginAudit);
                    await _ravenSession.SaveChangesAsync();
                }
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

        public async Task SendUserMessageStatus(HubMessageStatus messageStatus)
        {
            bool isChatStatusUpdate = false;
            var uid = Context.Items["uid"] as string;
            var chat = await _ravenSession.LoadAsync<Chat>(messageStatus.ChatId);
            var newStatus = mapper.Map<MessageStatus>(messageStatus);
            if (chat.MessageStatus == null)
            {
                chat.MessageStatus = newStatus;
                isChatStatusUpdate = true;
            }
            else
            {
                isChatStatusUpdate = chat.MessageStatus.Update(newStatus);
            }
            var member = chat.Members.Find(m => m.UserId == uid);
            if (member?.MessageStatus == null)
            {
                member.MessageStatus = newStatus;
            }
            else
            {
                member?.MessageStatus.Update(newStatus);
            }
            await _ravenSession.SaveChangesAsync();
            if (isChatStatusUpdate)
            {
                var receiveStatus = mapper.Map<ReceiveMessageStatus>(chat.MessageStatus);
                receiveStatus.ChatId = chat.Id;
                await Clients.Group(_loginedGroup).SendAsync(receiveStatus);
            }
        }
    }
}
