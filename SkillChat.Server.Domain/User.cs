using System;

namespace SkillChat.Server.Domain
{
    public class User
    {
        public string Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public DateTimeOffset RegisteredTime { get; set; }
        public override string ToString()
        {
            return $"{Id};{Login}";
        }
    }
}