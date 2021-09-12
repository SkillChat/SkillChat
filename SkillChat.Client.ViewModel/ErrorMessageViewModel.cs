using PropertyChanged;

namespace SkillChat.Client.ViewModel
{

    [AddINotifyPropertyChangedInterface]
    public class ErrorMessageViewModel
    {
        public ErrorMessageViewModel()
        {

        }

        /// <summary>
        /// Свойство, привязанное к свойству "Text" элемента, выводящего сообщения об ошибках во View.
        /// </summary>
        public string ErrorMsg { get; private set; }

        /// <summary>
        /// Св-во меняет визуальное отображение надписей и полей ввода при возникновении ошибок
        /// </summary>
        public bool IsError { get; set; } = false;

        /// <summary>
        /// Метод принимает статус или текст ошибки и инициализирует свойство ErrorMsg,
        /// привязанное к свойству "Text" элемента, выводящего сообщения об ошибках во View.
        /// </summary>
        /// <param name="message">код или текст ошибки</param>
        public void GetErrorMessage(string message)
        {
            switch (message)
            {
                case ErrorConnection:
                    ErrorMsg = "Сервер не доступен";
                    break;

                case ErrorAuthentication:
                    ErrorMsg = "Неверные Логин и/или Пароль";
                    break;

                case ErrorRegistration:
                    ErrorMsg = "Этот Логин уже существует";
                    break;

                default:
                    ErrorMsg = message;
                    break;
            }
        }

        /// <summary>
        /// Метод убирает с экрана текстовое сообщение об ошибке.
        /// </summary>
        public void ResetDisplayErrorMessage()
        {
            ErrorMsg = "";
        }

        public const string ErrorConnection = "500";
        public const string ErrorAuthentication = "404";
        public const string ErrorRegistration = "400";

    }
}
