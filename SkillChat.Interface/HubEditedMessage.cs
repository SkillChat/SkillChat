namespace SkillChat.Interface
{
    public class HubEditedMessage : HubMessage
    {
        public string Id { get; set; }
        public HubEditedMessage(string id, string chatId, string message)
        {
            Id = id;
            ChatId = chatId;
            Message = message;
        }
    }
}