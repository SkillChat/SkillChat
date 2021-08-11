using System;

namespace SkillChat.Interface
{
    public class ReceiveEditedMessage : ReceiveMessage
    {
        public DateTimeOffset LastEditTime { get; set; }
    }
}