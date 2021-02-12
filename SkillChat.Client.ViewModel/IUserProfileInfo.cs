using System;
using SkillChat.Server.ServiceModel.Molds;

namespace SkillChat.Client.ViewModel
{
    public interface IUserProfileInfo
    {
        public static  Action<UserProfileMold> UserProfileInfoEvent { get; set; }
    }
}
