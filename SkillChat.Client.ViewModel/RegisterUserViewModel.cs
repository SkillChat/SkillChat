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

        public string Password { get; set; }
        public bool Consent { get; set; }

        public string Login { get; set; }

        public ICommand RegisterCommand { get; }

    }
}
