using System.Collections.Generic;

namespace SkillChat.Interface
{
    public class HubMessage
    {
        public HubMessage() { }

        public HubMessage(string chatId, string message)
        {
            ChatId = chatId;
            Message = message;
        }

        public HubMessage(string chatId, string message, List<AttachmentHubMold> attachment)
        {
            ChatId = chatId;
            Message = message;
            Attachments = attachment;
        }

        public string Message { get; set; }
        public string ChatId { get; set; }
        public List<AttachmentHubMold> Attachments { get; set; }
    }
}
