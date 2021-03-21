using System.Threading.Tasks;
using SignalR.EasyUse.Interface;

namespace SkillChat.Interface
{
    public interface IChatHub: IServerMethods
    {
        Task SendMessage(string message, string chatId);
        Task UpdateUserDisplayName(string userDispalyName, string userId);
        Task Login(string token, string operatingSystem, string ipAddress, string nameVersionClient);
    }
}