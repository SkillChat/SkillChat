using System.Collections.Generic;
using System.Threading.Tasks;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using ServiceStack;
using SkillChat.Server.Domain;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;

namespace SkillChat.Server.ServiceInterface
{
    public class ChatService : Service
    {
        public IAsyncDocumentSession RavenSession { get; set; }

        [Authenticate]
        public async Task<MessagePage> Get(GetMessages request)
        {
            var messages = RavenSession.Query<Message>().OrderByDescending(x => x.PostTime);
            var result = new MessagePage();
            if(request.BeforePostTime != null)
            {
                messages = messages.Where(x => x.PostTime < request.BeforePostTime);
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
                    UserLogin = user?.Login??doc.UserId,                    
                };
                result.Messages.Add(message);
            }
            return result;
        }
    }
}