using System.Collections.Generic;
using System.Threading.Tasks;
using SignalR.EasyUse.Interface;

namespace SkillChat.Interface
{
    public interface IChatHub: IServerMethods
    {
        Task SendMessage(string message, string chatId);
        Task Login(string token);
        Task SendStatuses(List<MessageStatus> statuses);
    }
}