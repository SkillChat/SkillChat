﻿using System;
using SignalR.EasyUse.Interface;

namespace SkillChat.Interface
{
    public class ReceiveMessage: IClientMethod
    {
        public string Id { get;set; }
        public string UserId { get;set; }
        public string UserLogin { get;set; }
        public string UserNickname { get;set; }
        public string Message{ get;set; }
        public DateTimeOffset PostTime { get;set; }
        public string ChatId { get; set; }
    }
}
