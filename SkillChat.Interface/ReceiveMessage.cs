using SignalR.EasyUse.Interface;

namespace SkillChat.Interface
{
    public class ReceiveMessage: IClientMethod
    {
        public string User { get;set; }
        public string Message{ get;set; }
    }
}
