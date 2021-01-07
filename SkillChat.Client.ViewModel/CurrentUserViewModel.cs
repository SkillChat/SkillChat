using PropertyChanged;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace SkillChat.Client.ViewModel
{
    /// <summary>
    /// Текущий пользователь
    /// </summary>
    [AddINotifyPropertyChangedInterface]
   public class CurrentUserViewModel
    {
        public CurrentUserViewModel()
        {
            IsPassword = false;
            PassError = false;
        }
        public string UserName { get; set; }

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
        /// <summary>
        /// Состояние для возможности отображения - произошла ошибка
        /// </summary>
        public bool PassError { get; set; }
        /// <summary>
        /// Текст ошибки
        /// </summary>
        public string ErrorMsg { get; set; }        
        /// <summary>
        /// Сброс свойств на исходные
        /// </summary>
        public void Reset()
        {
            PassError = false;
            ErrorMsg = "";
        }
        /// <summary>
        /// Изменение при ошибке
        /// </summary>
        /// <param name="errorMsg">Текст сообщения об ошибке</param>
        public void Error(string errorMsg)
        {
            PassError = true;
            ErrorMsg = errorMsg;
        }
    }
}
