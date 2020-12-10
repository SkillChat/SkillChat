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
            CurrentColor = BaseColor;
            ForegroundColor = BaseForegroundColor;
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
        /// Состояние для возможности отобажения пароля
        /// </summary>
        public bool IsPassword { get; set; }
        /// <summary>
        /// Состояние для возможности отобажения - произошла ошибкаы
        /// </summary>
        public bool PassError { get; set; }
        /// <summary>
        /// Текст ошибки
        /// </summary>
        public string ErrorMsg { get; set; }
        /// <summary>
        /// текущий Цвет для TerxtBox
        /// </summary>
        public string CurrentColor { get; set; }
        /// <summary>
        /// Цвет текста
        /// </summary>
        public string ForegroundColor { get; set; }
        /// <summary>
        /// Цвет при ошибке
        /// </summary>
        public readonly string ErrorColor = "#FFA85E";
        /// <summary>
        /// Базовый цвет
        /// </summary>
        public readonly string BaseColor = "#9976FB";
        /// <summary>
        /// Цвет foreground
        /// </summary>
        public readonly string BaseForegroundColor = "#7F57F0";
        /// <summary>
        /// Цвет foregroundпри ошибке
        /// </summary>
        public readonly string ErrorForegroundColor = "#FFC797";
        /// <summary>
        /// Сброс свойств при 
        /// </summary>
        public void Reset()
        {
            PassError = false;
            CurrentColor = BaseColor;
            ErrorMsg = "";
            ForegroundColor = BaseForegroundColor;
        }

        public void Error(string errorMsg)
        {
            PassError = true;
            CurrentColor = ErrorColor;
            ForegroundColor = ErrorForegroundColor;
            ErrorMsg = errorMsg;
        }

    }
}
