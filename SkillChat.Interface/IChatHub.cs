using SignalR.EasyUse.Interface;
﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkillChat.Interface
{
    public interface IChatHub : IServerMethods
    {
        Task SendMessage(HubMessage hubMessage);
        Task UpdateMyDisplayName(string userDispalyName);
        Task Login(string token, string operatingSystem, string ipAddress, string nameVersionClient);
        Task SendStatuses(List<MessageStatus> statuses);
    }
}
