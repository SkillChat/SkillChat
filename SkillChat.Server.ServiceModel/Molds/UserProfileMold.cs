using System.ComponentModel;

namespace SkillChat.Server.ServiceModel.Molds
{
    [Description("Профиль пользователя")]
    public class UserProfileMold
    {
        [Description("Идентификатор пользователя")]
        public string UserId { get; set; }
        
        [Description("Псевдоним пользователя")]
        public string Login { get; set; }

        [Description("Наличие пароля")]
        public bool IsPasswordSetted { get; set; }
    }
}