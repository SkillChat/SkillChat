using SignalR.EasyUse.Interface;

namespace SkillChat.Interface
{
    public class UpdateUserDisplayName : IClientMethod
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string UserLogin { get; set; }
    }
}
