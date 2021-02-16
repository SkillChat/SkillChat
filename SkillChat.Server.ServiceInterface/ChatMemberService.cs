using System;
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
    public class ChatMemberService : Service
    {
        public IAsyncDocumentSession RavenSession { get; set; }
        public IMapper Mapper { get; set; }

        [Authenticate]
        public async Task<ChatMemberMold> Post(SetChatMember request)
        {
            var session = Request.ThrowIfUnauthorized();
            var messagesHistoryBeginDate = request.MessagesHistoryDateBegin;

            var chatMember = await RavenSession.LoadAsync<ChatMemberMold>(session.UserAuthId);
            chatMember.MessagesHistoryDateBegin = (DateTimeOffset)messagesHistoryBeginDate;

            await RavenSession.StoreAsync(chatMember);
            await RavenSession.SaveChangesAsync();

            var mapped = Mapper.Map<ChatMemberMold>(chatMember);
            return mapped;
        }


    }
}