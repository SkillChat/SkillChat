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
   public class CurrentUserViewModel : ICurrentUser
    {
        public CurrentUserViewModel()
        {
            IsPassword = false;
            PassError = false;
            IsServerStateHistory = false;
        }

        public string UserName { get; set; }
        
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
        /// <summary>
        /// Состояние для возможности отображения - произошла ошибка
        /// </summary>
        public bool PassError { get; set; }
        /// <summary>
        /// Текст ошибки
        /// </summary>
        public string ErrorMsg { get; set; }

        /// <summary>
        /// Флаг - подключение с сервером отсутствует: Сервер только подключается - false, подключение разорвано - true
        /// </summary>
        public bool IsServerStateHistory { get; set; }

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

            // это условие срабатывает, когда сервер отключается во время работы программы,
            // программа выкидывает пользователя на страницу входа, сообщает, что сервер не доступен,
            // но пользователь жмёт на кнопку "Войти"
            if (errorMsg.Substring(0, 11) == "Подключение" && IsServerStateHistory == true)
            {
                ErrorMsg = "Сервер не доступен";
            }
            // это условие срабатывает при запуске, когда клиент уже запущен, а соединение с сервером в процессе установки
            else if (errorMsg.Substring(0, 11) == "Подключение")
            {
                ErrorMsg = "";
            }
            // это условие срабатывает, когда сервер отключается во время работы программы
            else
            {
                ErrorMsg = errorMsg;
            }
            
        }
    }
}
