using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace SkillChat.Client.ViewModel
{
    public class MenuItemObject : MenuObservableObject
    {
        private ICommand _command;
        private string _content;

        public ICommand Command
        {
            get { return _command; }
            set
            {
                _command = value;
                OnPropertyChanged();
            }
        }

        public string Content
        {
            get { return _content; }
            set
            {
                _content = value;
                OnPropertyChanged();
            }
        }
    }
}
