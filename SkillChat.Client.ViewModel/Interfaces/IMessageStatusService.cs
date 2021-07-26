using SkillChat.Client.ViewModel.Models;

namespace SkillChat.Client.ViewModel.Interfaces
{
    public interface IMessageStatusService
    {
        void SetMyIncomingMessagesStatus(MessageStatusModel status);
        void ReceivedChatMessageStatus(MessageStatusModel status);
        void SetIncomingMessageReadStatus(MessageViewModel message);
        void SetIncomingMessageReсeivedStatus(MessageViewModel message);
        void SetStatusToMyMessage(MyMessageViewModel message);
    }
}