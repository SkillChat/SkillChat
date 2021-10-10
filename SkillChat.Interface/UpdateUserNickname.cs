using SignalR.EasyUse.Interface;

namespace SkillChat.Interface
{
    public class UpdateUserNickname : IClientMethod
    {
        public string UserId { get; set; }
        public string UserNickname { get; set; }
        public string UserLogin { get; set; }
    }
}
