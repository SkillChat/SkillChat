using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace SkillChat.Client.ViewModel
{
    public class MenuItemObject : ReactiveObject
    {     
        private ICommand _command;
        private string _content;

        public ICommand Command
        {
            get { return _command; }
            set
            {
                this.RaiseAndSetIfChanged(ref _command, value);
            }
        }

        public string Content
        {
            get { return _content; }
            set
            {
                this.RaiseAndSetIfChanged(ref _content, value);
            }
        }
    }
}
