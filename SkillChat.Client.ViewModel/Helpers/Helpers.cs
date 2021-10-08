using System;
using System.Collections.Generic;
using System.Text;

namespace SkillChat.Client.ViewModel.Helpers
{
    public static class Helpers
    {
        public static string NameOrLogin(string UserName, string UserLogin)
        {
            return string.IsNullOrEmpty(UserName?.Trim()) ? UserLogin : UserName;
        }
    }


}
