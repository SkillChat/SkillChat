using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using ServiceStack;
using SkillChat.Server.Domain;
using SkillChat.Server.Hubs;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.ServiceModel.Molds.Chats;

namespace SkillChat.Server.ServiceInterface
{
    public class ChatService : Service
    {
        public IAsyncDocumentSession RavenSession { get; set; }
        public IMapper Mapper { get; set; }
        public IHubContext<ChatHub> Hub { get; set; }

        public async Task Get(GetTest request)
        {
            var lifetimeManagerInfo = Hub.GetType().GetField("_lifetimeManager", BindingFlags.NonPublic | BindingFlags.Instance);
            var lifetimeManager = lifetimeManagerInfo.GetValue(Hub);
            var connectionsInfo = lifetimeManager.GetType().GetField("_connections", BindingFlags.NonPublic | BindingFlags.Instance);
            var connectionsValue = connectionsInfo.GetValue(lifetimeManager);
            var connectionContextsInfo = connectionsValue.GetType().GetField("_connections", BindingFlags.NonPublic | BindingFlags.Instance);
            var connectionContexts = connectionContextsInfo.GetValue(connectionsValue) as IDictionary<string, HubConnectionContext>;
            foreach (var pair in connectionContexts)
            {
                pair.Value.Items["nickname"] = "Hacked!";
            }
        }


        [Authenticate]
        public async Task<MessagePage> Get(GetMessages request)
        {
            var messages = RavenSession.Query<Message>().Where(e => e.ChatId == request.ChatId).OrderByDescending(x => x.PostTime);
            var result = new MessagePage();
            if (request.BeforePostTime != null)
            {
                messages = messages.Where(x => x.PostTime.UtcDateTime < request.BeforePostTime.Value.UtcDateTime);
            }
            var pageSize = request.PageSize ?? 10;
            var docs = await messages.Take(pageSize).Include(x => x.UserId).ToListAsync();
            result.Messages = new List<MessageMold>();
            foreach (var doc in docs)
            {
                var user = await RavenSession.LoadAsync<User>(doc.UserId);
                var message = Mapper.Map<MessageMold>(doc);
                if (user != null)
                {
                    message.UserNickName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Login : user.DisplayName;
                }
                result.Messages.Add(message);
            }
            return result;
        }

        [Authenticate]
        public async Task<ChatPage> Get(GetChatsList request)
        {
            var session = Request.ThrowIfUnauthorized();
            var uid = session.UserAuthId;
            var chats = await RavenSession.Query<Chat>()
                .Where(chat => chat.Members.Any(member => member.UserId == uid))
                .ToListAsync();
            return new ChatPage
            {
                Chats = chats.Select(e => Mapper.Map<ChatMold>(e)).ToList()
            };
        }
    }
}