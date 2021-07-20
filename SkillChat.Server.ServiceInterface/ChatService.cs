using System;
using AutoMapper;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using ServiceStack;
using SkillChat.Server.Domain;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.ServiceModel.Molds.Attachment;
using SkillChat.Server.ServiceModel.Molds.Chats;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sparrow.Logging;

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
            
            var docs = 
                await messages.Take(pageSize)
                    .Include(x => x.UserId)
                    .Include(s => s.Attachments)
                    .ToListAsync();

            var chat = await RavenSession.LoadAsync<Chat>(request.ChatId);
            result.ChatMessageStatus = Mapper.Map<MessageStatusMold>(chat.MessageStatus);
            try
            {
                var member = chat.Members.FirstOrDefault(m => m.UserId == uid);
                result.MemberMessageStatus = Mapper.Map<MessageStatusMold>(member?.MessageStatus);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка во время получения чат мембера!!!\n" + ex.Message);
            }
            result.Messages = new List<MessageMold>();
            foreach (var doc in docs)
            {
                var user = await RavenSession.LoadAsync<User>(doc.UserId);
                var message = Mapper.Map<MessageMold>(doc);

                if (doc.Attachments != null)
                {
                    var attach = await RavenSession.LoadAsync<Attachment>(doc.Attachments);
                    message.Attachments = new List<AttachmentMold>();

                    foreach (var id in doc.Attachments)
                    {
                        if (attach.TryGetValue(id, out var attachment))
                        {
                            message.Attachments.Add(Mapper.Map<AttachmentMold>(attachment));
                        }
                        //TODO учесть в будущем показ потерянных файлов
                    }
                }

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