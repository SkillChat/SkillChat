using System.Collections.Generic;

namespace SkillChat.Interface
{
    public class HubMessage
    {
        public HubMessage() { }

        public HubMessage(string chatId, string message,string idReplyMessage)
        {
            ChatId = chatId;
            Message = message;
            IdReplyMessage = idReplyMessage;
        }

        public HubMessage(string chatId, string message, List<AttachmentHubMold> attachment, string idReplyMessage)
        {
            ChatId = chatId;
            Message = message;
            Attachments = attachment;
            IdReplyMessage = idReplyMessage;
        }

        public string Message { get; set; }
        public string ChatId { get; set; }
        public List<AttachmentHubMold> Attachments { get; set; }
        public string IdReplyMessage { get; set; }
    }
}
