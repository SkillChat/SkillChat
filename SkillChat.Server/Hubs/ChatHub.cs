﻿using Microsoft.AspNetCore.SignalR;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Serilog;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Host;
using SignalR.EasyUse.Server;
using SkillChat.Interface;
using SkillChat.Server.Domain;
using System;
using System.Collections.Generic;
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

                await Groups.AddToGroupAsync(this.Context.ConnectionId, logOn.Id);

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

        /// <summary>Получение листа статусов сообщений и рассылка отправителям сообдений об их статусах</summary>
        /// <param name="statuses">Лист статусов сообщеий</param>
        public async Task SendStatuses(List<MessageStatus> statuses)
        { 
            var uid = Context.Items["uid"] as string; //Получил ID юзера отправившего статус
            //Загружаем все статусы этого юзера
            var statusItems = _ravenSession.Query<MessageStatusDomain>().Where(s => s.UserId == uid);
            foreach (var status in statuses) //Перебираем все статусы в поиске присланного
            {
                //Находим статус для этого сообщения
                var statusItem = await statusItems.FirstOrDefaultAsync(s => s.MessageId == status.MessageId);
                //Получаем сообщение для которого пришел статус
                var statusedMessage = await _ravenSession.LoadAsync<Message>(status.MessageId);
                if (statusItem == null)//Если нет статуса для требуемого сообщения, то создаем новый
                {
                    statusItem = new MessageStatusDomain()
                    {
                        UserId = uid,
                        MessageId = status.MessageId,
                        ReadDate = status?.ReadDate,
                        ReceivedDate = status?.ReceivedDate,
                    };
                    await _ravenSession.StoreAsync(statusItem); //Сохранил статус в БД
                    //Далее создаем счетчики для сообщений
                    if (status?.ReadDate != null)
                    {
                        _ravenSession.CountersFor(statusedMessage).Increment(MessageCounters.ReadCounter.ToString());
                    }
                    if (status?.ReceivedDate != null)
                    {
                        _ravenSession.CountersFor(statusedMessage).Increment(MessageCounters.ReceivedCounter.ToString());
                    }
                }
                else //Если статус найден
                {
                    //Если даты прочтения нет, то присваиваем ее
                    if (statusItem.ReadDate == null && status?.ReadDate != null)
                    {
                        statusItem.ReadDate = status.ReadDate;
                        // и увеличиваем счетчик прочтений
                        _ravenSession.CountersFor(statusedMessage).Increment(MessageCounters.ReadCounter.ToString());
                    }
                    //Если даты получения нет, то присваиваем ее
                    if (statusItem.ReceivedDate == null && status?.ReceivedDate != null)
                    {
                        statusItem.ReceivedDate = status.ReceivedDate;
                        // и увеличиваем счетчик получений
                        _ravenSession.CountersFor(statusedMessage).Increment(MessageCounters.ReceivedCounter.ToString());
                    }
                }

                var counters = await _ravenSession.CountersFor(statusedMessage).GetAllAsync();
                long? read = counters?.GetValueOrDefault("ReadCounter");
                long? rec = counters?.GetValueOrDefault("ReceivedCounter");
                //if (counters?.GetValueOrDefault(MessageCounters.ReadCounter.ToString()) == 1
                //|| counters?.GetValueOrDefault(MessageCounters.ReadCounter.ToString()) == 1)

                if (read == 1 || rec == 1)
                {
                    await Clients.Group(statusedMessage.UserId).SendAsync(status);
                }
            }
            //Сохраняем изменения в БД
            await _ravenSession.SaveChangesAsync();
        }
    }
}
