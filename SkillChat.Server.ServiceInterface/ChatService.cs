using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
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
        public IMapper Mapper { get; set; }

        [Authenticate]
        public async Task<MessagePage> Get(GetMessages request)
        {
            var session = Request.ThrowIfUnauthorized();
            var uid = session.UserAuthId;
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
                if (user?.Login!= null)
                {
                    message.UserLogin = user.Login;
                }

                var isMy = doc.UserId == uid ? true : false;
                var status = await RavenSession.Query<MessageStatusDomain>()
                    .FirstOrDefaultAsync(m => m.MessageId == message.Id && m.UserId == uid);

                if (isMy)
                {
                    var counters = await RavenSession.CountersFor(doc).GetAllAsync();
                    long? readCounter = null;
                    long? receivedCounter = null;
                    if (counters.Count > 0)
                    {
                        counters.TryGetValue(MessageCounters.ReadCounter.ToString(), out readCounter);
                        counters.TryGetValue(MessageCounters.ReceivedCounter.ToString(), out receivedCounter);
                    }
                    message.ReadCount = readCounter;
                    message.ReceivedCount = receivedCounter;
                }
                else
                {
                    if (status != null)
                    {
                        message.Read = status.ReadDate != null;
                        message.Received = status.ReceivedDate != null;
                    }
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