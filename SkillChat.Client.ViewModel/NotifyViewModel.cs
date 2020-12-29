using ReactiveUI;
using SkillChat.Client.ViewModel;

namespace SkillChat.Client.Notification.ViewModels
{
  public  class NotifyViewModel: ReactiveObject, IScreen
    {
        private string _userLogin = "";
        private string _text = "";
        private INotify _window;

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

        public NotifyViewModel(string title, string text, INotify window)
        {
            (UserLogin, Text, _window) = (title, text, window);
        }

        public RoutingState Router { get; } = new RoutingState();
    }
}
