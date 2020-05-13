using System.ComponentModel;

namespace SkillChat.Server.ServiceModel.Molds
{
    [Description("Пара токенов авторизации")]
    public class TokenResult
    {
        [Description("Токен для авторизованных запросов")]
        public string AccessToken { get; set; }
        [Description("Токен для обновления токенов")]
        public string RefreshToken { get; set; }
    }
}
