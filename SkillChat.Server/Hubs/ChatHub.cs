using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SignalR.EasyUse.Server;
using SkillChat.Interface;

namespace SkillChat.Server.Hubs
{
    public class ChatHub : Hub, IChatHub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync(new ReceiveMessage
            {
                User = user,
                Message = message,
            });
        }
    }
}
