using System.ComponentModel;

namespace SkillChat.Server.ServiceModel.Molds
{
    [Description("Профиль пользователя")]
    public class UserProfileMold
    {
        [Description("Идентификатор пользователя")]
        public string Id { get; set; }

        [Description("Псевдоним пользователя")]
        public string Login { get; set; }

        [Description("Имя пользователя")]
        public string DisplayName { get; set; }
    }
}