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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            var userId = session?.UserAuthId;
            Chat chat = await RavenSession.LoadAsync<Chat>(request.ChatId);
            ChatMember chatMember = chat.Members.FirstOrDefault(e => e.UserId == userId);
            var visibleMessages = GetVisibleMessagesQuery(request.ChatId, userId, chatMember);
            var messages = visibleMessages.OrderByDescending(x => x.PostTime);
            var result = new MessagePage();

            if (request.BeforePostTime != null)
            {
                messages = messages.Where(x => x.PostTime.UtcDateTime < request.BeforePostTime.Value.UtcDateTime);
            }

            var pageSize = request.PageSize ?? 50;
            
            // Include() pre-loads related documents into the RavenDB session cache.
            // Subsequent LoadAsync calls for these documents will hit the cache, not the database.
            var docs = 
                await messages.Take(pageSize + 1)
                    .Include(x => x.UserId)          // Pre-loads User documents for message authors
                    .Include(s => s.Attachments)     // Pre-loads Attachment documents for messages
                    .Include(i => i.IdQuotedMessage) // Pre-loads quoted Message documents
                    .ToListAsync();

            result.HasMoreBefore = docs.Count > pageSize;
            docs = docs.Take(pageSize).ToList();

            // Batch pre-load quoted message users and their attachments to avoid N+1 queries.
            // The Include() above only loads the quoted Message documents, not their related User/Attachment documents.
            var quotedMessageIds = docs
                .Where(d => !d.IdQuotedMessage.IsNullOrEmpty())
                .Select(d => d.IdQuotedMessage)
                .Distinct()
                .ToList();
            
            if (quotedMessageIds.Any())
            {
                // Load all quoted messages (will hit session cache due to Include above)
                var quotedMessages = await RavenSession.LoadAsync<Message>(quotedMessageIds);
                
                // Collect user IDs from quoted messages and batch pre-load into session cache
                var quotedUserIds = quotedMessages.Values
                    .Where(m => m != null && !string.IsNullOrEmpty(m.UserId))
                    .Select(m => m.UserId)
                    .Distinct()
                    .ToList();
                
                if (quotedUserIds.Any())
                {
                    await RavenSession.LoadAsync<User>(quotedUserIds);
                }
                
                // Collect attachment IDs from quoted messages and batch pre-load into session cache
                var quotedAttachmentIds = quotedMessages.Values
                    .Where(m => m?.Attachments != null)
                    .SelectMany(m => m.Attachments)
                    .Distinct()
                    .ToList();
                
                if (quotedAttachmentIds.Any())
                {
                    await RavenSession.LoadAsync<Attachment>(quotedAttachmentIds);
                }
            }

            if (chatMember?.LastReadMessagePostTime is DateTimeOffset lastReadMessagePostTime)
            {
                result.FirstUnreadMessageId = (await visibleMessages
                    .Where(message => message.UserId != userId && message.PostTime > lastReadMessagePostTime)
                    .OrderBy(message => message.PostTime)
                    .FirstOrDefaultAsync())
                    ?.Id;
            }

            result.Messages = new List<MessageMold>();
            foreach (var doc in docs)
            {
                // User is already in session cache due to Include(x => x.UserId)
                var user = await RavenSession.LoadAsync<User>(doc.UserId);
                var message = Mapper.Map<MessageMold>(doc);

                // Attachments are already in session cache due to Include(s => s.Attachments)
                message = await GetAttachments(doc, message);

                if (!doc.IdQuotedMessage.IsNullOrEmpty())
                {
                    // Quoted message is already in session cache due to Include(i => i.IdQuotedMessage)
                    var mes = await RavenSession.LoadAsync<Message>(doc.IdQuotedMessage);

                    message.QuotedMessage = Mapper.Map<MessageMold>(mes);
                    // Quoted message's user is pre-loaded in batch above into session cache
                    var userQuitedMessage = await RavenSession.LoadAsync<User>(message.QuotedMessage.UserId);

                    // Quoted message's attachments are pre-loaded in batch above into session cache
                    message.QuotedMessage = await GetAttachments(mes, message.QuotedMessage);

                    if (userQuitedMessage != null)
                    {
                        message.QuotedMessage.UserNickName = string.IsNullOrWhiteSpace(userQuitedMessage.DisplayName)
                            ? userQuitedMessage.Login : userQuitedMessage.DisplayName;
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

        private IRavenQueryable<Message> GetVisibleMessagesQuery(string chatId, string userId, ChatMember chatMember)
        {
            var messages = RavenSession.Query<Message>()
                .Where(message => message.ChatId == chatId);

            if (chatMember != null)
            {
                messages = messages.Where(message => message.PostTime > chatMember.MessagesHistoryDateBegin);
            }

            return messages.Where(message => message.HideForUsers == null || !message.HideForUsers.Contains(userId));
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
        /// <summary>
        /// Получает все вложения сообщения, если они есть.
        /// Attachments are expected to be pre-loaded into the session cache via Include() or batch LoadAsync.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageMold"></param>
        /// <returns></returns>
        private async Task<MessageMold> GetAttachments(Message message, MessageMold messageMold)
        {
            if (message.Attachments != null)
            {
                // LoadAsync will hit the session cache if attachments were pre-loaded via Include() or batch load
                var attach = await RavenSession.LoadAsync<Attachment>(message.Attachments);
                messageMold.Attachments = new List<AttachmentMold>();

                foreach (var id in message.Attachments)
                {
                    if (attach.TryGetValue(id, out var attachment))
                    {
                        messageMold.Attachments.Add(Mapper.Map<AttachmentMold>(attachment));
                    }
                    //TODO учесть в будущем показ потерянных файлов
                }
            }
            return messageMold;
        }
    }
}
