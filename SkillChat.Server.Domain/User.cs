using System;

namespace SkillChat.Server.Domain
{
    public class User
    {
        public string Id { get; set; }
        public string Login { get; set; }
        public string Nickname { get; set; }
        public string AboutMe { get; set; }
        public DateTimeOffset RegisteredTime { get; set; }

        public override string ToString()
        {
            return $"{Id};{Login}";
        }
    }

    public class UserSecret
    {
        public string Id { get; set; }
        public string Password { get; set; }
        public byte[] Salt { get; set; }
    }
}