using System.Threading.Tasks;
using SignalR.EasyUse.Interface;

namespace SkillChat.Interface
{
    public interface IChatHub: IServerMethods
    {
        Task SendMessage(string message);
        Task Login(string token);
    }
}