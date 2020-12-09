using System.Collections.Generic;
using System.Threading.Tasks;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using ServiceStack;
using SkillChat.Server.Domain;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.ServiceModel.Molds.Chats;

namespace SkillChat.Server.ServiceInterface
{
    public class ChatService : Service
    {
        public IAsyncDocumentSession RavenSession { get; set; }

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
                var message = new MessageMold
                {
                    Id = doc.Id,
                    PostTime = doc.PostTime,
                    Text = doc.Text,
                    UserLogin = user?.Login ?? doc.UserId,
                    ChatId = doc.ChatId,
                };
                result.Messages.Add(message);
            }
            return result;
        }

        public async Task<ChatPage> Get(GetChatsList request)
        {
            var chats = await RavenSession.Query<Chat>().Select(e => new ChatMold()
            {
                ChatType = (ChatTypeMold)(int)e.ChatType,
                Id = e.Id,
                OwnerId = e.OwnerId,
                ChatName = e.ChatName
            }).ToListAsync();
            return new ChatPage() { Chats = chats };
        }
    }
}