using SignalR.EasyUse.Interface;
using System.Threading.Tasks;

namespace SkillChat.Interface
{
    public interface IChatHub : IServerMethods
    {
        Task SendMessage(HubMessage hubMessage);
        Task UpdateMessage(HubEditedMessage hubEditedMessage);
        Task UpdateMyNickname(string nickname);
        Task Login(string token, string operatingSystem, string ipAddress, string nameVersionClient);
    }
}