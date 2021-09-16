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

        public async Task SendMessage(HubMessage hubMessage, string IdReplyMessage)
        {
            var messageItem = new Message
            {
                UserId = Context.Items["uid"] as string,
                Text = hubMessage.Message.Trim(),
                PostTime = DateTimeOffset.UtcNow,
                ChatId = hubMessage.ChatId,
                Attachments = hubMessage.Attachments?.Select(s => s.Id).ToList(),
                IdReplyMessage = IdReplyMessage
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
                Attachments = hubMessage.Attachments,
                IdReplyMessage = IdReplyMessage
            });

            var logMessage = IdReplyMessage.IsNullOrEmpty() ? $"User {Context.Items["nickname"]}({Context.Items["login"]}) send message in main chat" :
                                                                              $"User {Context.Items["nickname"]}({Context.Items["login"]}) responded to the message in main chat";
            Log.Information(logMessage);
        }

        public async Task UpdateMessage(HubEditedMessage hubEditedMessage, string IdReplyMessage)
        {
            try
            {
                var mes = await _ravenSession.LoadAsync<Message>(hubEditedMessage.Id);

                if (mes.Text.Trim()!=hubEditedMessage.Message.Trim()||mes.IdReplyMessage!=IdReplyMessage)
                {
                    mes.Text = hubEditedMessage.Message.Trim();
                    mes.LastEditTime = DateTimeOffset.Now;
                    mes.IdReplyMessage = IdReplyMessage;
                    await _ravenSession.SaveChangesAsync();

                    await Clients.Group(_loginedGroup).SendAsync(new ReceiveEditedMessage()
                    {
                        Id = hubEditedMessage.Id,
                        Message = mes.Text,
                        LastEditTime = mes.LastEditTime.Value,
                        IdReplyMessage=IdReplyMessage
                    });
                    Log.Information($"User {Context.Items["nickname"]}({Context.Items["login"]}) edited message in main chat");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
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

                var logOn = new LogOn
                {
                    Id = jwtPayload["sub"],
                    UserLogin = jwtPayload["name"],
                    Error = LogOn.LogOnStatus.Ok,
                };

                var user = await _ravenSession.LoadAsync<User>(jwtPayload["sub"]);
                if (user != null)
                {
                    Context.Items["nickname"] = user.DisplayName;
                }
                else
                {
                    //Если пользователя нет в БД
                    await Clients.Caller.SendAsync(new LogOn
                    {
                        Error = LogOn.LogOnStatus.ErrorUserNotFound
                    });
                    throw new TokenException("User not found");
                }

                if (long.TryParse(jwtPayload["exp"], out long expire))
                {
                    logOn.ExpireTime = DateTimeOffset.FromUnixTimeSeconds(expire);
                    if (logOn.ExpireTime < DateTimeOffset.UtcNow)
                    {
                        //Если время жизни токена закончилось
                        await Clients.Caller.SendAsync(new LogOn
                        {
                            Error = LogOn.LogOnStatus.ErrorExpiredToken
                        });
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
                Log.Warning($"Bad token from connection {Context.ConnectionId}");
            }
        }
    }
}
