using System;
using SkillChat.Interface;

namespace SkillChat.Client.ViewModel.Services
{
    public interface IUserMessagesStatusService
    {
        void UpdateReadedStatus(string id, DateTimeOffset date);
        void UpdateReceivedStatus(string id, DateTimeOffset date);
        void SetSendStatusMethod(Action<HubUserMessageStatus> sendMethod);
    }
}