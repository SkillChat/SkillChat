using SignalR.EasyUse.Interface;
using System.Threading.Tasks;

namespace SkillChat.Interface
{
    public interface IChatHub : IServerMethods
    {
        Task SendMessage(HubMessage hubMessage, string IdReplyMessage);
        Task UpdateMessage(HubEditedMessage hubEditedMessage, string IdReplyMessage);
        Task UpdateMyDisplayName(string userDispalyName);
        Task Login(string token, string operatingSystem, string ipAddress, string nameVersionClient);
    }
}