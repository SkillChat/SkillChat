using PropertyChanged;
using System;
using SkillChat.Client.ViewModel.Helpers;

namespace SkillChat.Client.ViewModel
{
    /// <summary>
    /// Текущий пользователь
    /// </summary>
    [AddINotifyPropertyChangedInterface]
   public class CurrentUserViewModel : ICurrentUser
    {
        public CurrentUserViewModel()
        {
            IsPassword = false;
            ErrorMessageLoginPage = new ErrorMessageViewModel();
        }

        public string Nickname { get; set; }

        public string DisplayName => Nickname.FallbackIfEmpty(Login);

        public string Login { get; set; }

        public string Id { get; set; }

        string password;
        public string Password
        {
            get { return password; }
            set
            {
                IsPassword = !String.IsNullOrWhiteSpace(value);
                password = value;
            }
        }
        /// <summary>
        /// Состояние для возможности отображения пароля
        /// </summary>
        public bool IsPassword { get; set; }

        public ErrorMessageViewModel ErrorMessageLoginPage { get; set; }
    }
}
