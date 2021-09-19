using System;
using SkillChat.Client.ViewModel.Models;
using SkillChat.Interface;

namespace SkillChat.Client.ViewModel.Services
{
    public interface IUserMessagesStatusService
    {
        void UpdateReadedStatus(string id, DateTimeOffset date);
        void UpdateReceivedStatus(string id, DateTimeOffset date);
        void SetSendStatusMethod(Action<HubUserMessageStatus> sendMethod);
        void LoadUserStatus(UserMessageStatusModel m);
    }
}