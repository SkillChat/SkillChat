using SignalR.EasyUse.Interface;
using System.Threading.Tasks;

namespace SkillChat.Interface
{
    public interface IChatHub : IServerMethods
    {
        Task SendMessage(HubMessage hubMessage);
        Task UpdateMyDisplayName(string userDispalyName);
        Task Login(string token, string operatingSystem, string ipAddress, string nameVersionClient);
        Task SendUserMessageStatus(HubMessageStatus messageStatus);
    }
}