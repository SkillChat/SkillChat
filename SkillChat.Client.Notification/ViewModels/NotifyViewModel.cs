using System;
using System.Collections.Generic;
using System.Text;
using ReactiveUI;
using SkillChat.Client.Notification.Views;

namespace SkillChat.Client.Notification.ViewModels
{
  public  class NotifyViewModel: ReactiveObject, IScreen
    {
        private string _userLogin = "";
        private string _text = "";
        private Notify _window;

        public string UserLogin
        {
            get => _userLogin;
            private set => this.RaiseAndSetIfChanged(ref _userLogin, value);
        }

        public string Text
        {
            get => _text;
            private set => this.RaiseAndSetIfChanged(ref _text, value);
        }

        public NotifyViewModel(string title, string text, Notify window)
        {
            (UserLogin, Text, _window) = (title, text, window);
        }

        public RoutingState Router { get; } = new RoutingState();
    }
}
