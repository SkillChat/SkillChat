using PropertyChanged;
using System;

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

        public string UserName { get; set; }

        public string DisplayName => Helpers.Helpers.NameOrLogin(UserName, Login);

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
