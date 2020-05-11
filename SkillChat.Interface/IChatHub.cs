using System.Threading.Tasks;
using SignalR.EasyUse.Interface;

namespace SkillChat.Interface
{
    public interface IChatHub: IServerMethods
    {
        Task SendMessage(string user, string message);
    }
}