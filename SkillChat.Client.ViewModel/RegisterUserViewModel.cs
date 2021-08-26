using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class RegisterUserViewModel
    {
        public RegisterUserViewModel()
        {
            ErrorMessageRegisterPage = new ErrorMessageViewModel();
        }

        public string Password { get; set; }
        public bool Consent { get; set; }

        public string Login { get; set; }
        public string UserName { get; set; }

        public ICommand RegisterCommand { get; }
        public ICommand GoToLoginCommand { get; set; }

        public ErrorMessageViewModel ErrorMessageRegisterPage { get; set; }

    }
}
