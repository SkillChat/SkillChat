using System;
using SignalR.EasyUse.Interface;

namespace SkillChat.Interface
{
    public class LogOn : IClientMethod
    {
        public string Id { get; set; }
        public string UserLogin { get; set; }
        public DateTimeOffset ExpireTime { get; set; }
        public bool Error { get; set; }
    }
}