using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Windows.Input;
using PropertyChanged;
using ReactiveUI;
using ServiceStack;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class SettingsViewModel
    {
        public SettingsViewModel(IJsonServiceClient serviceClient)
        {
            ChatSettings = new UserChatSettings();
            OpenSettingsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                IsHeaderMenuPopup = !IsHeaderMenuPopup;
                IsHeaderMenuPopupEvent?.Invoke(IsHeaderMenuPopup);
            });

            WindowSettingsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                IsHeaderMenuPopup = false;
                IsWindowSettings = !IsWindowSettings;
                IsWindowSettingsEvent?.Invoke(IsWindowSettings);
                var settings = await serviceClient.GetAsync(new GetMySettings());

            GoToSettingsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                Audit = false;
                Settings = true;
                var settings = await serviceClient.GetAsync(new GetMySettings());
                if (settings.SendingMessageByEnterKey)
                {
                    TypeEnter = settings.SendingMessageByEnterKey;
                    TypeEnterEvent?.Invoke(TypeEnter);
                }
            });

            SettingsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var settings = await serviceClient.PostAsync(new SetSettings {SendingMessageByEnterKey = TypeEnter});
                ChatSettings = settings;
            });

            MorePointerPressedCommand = ReactiveCommand.Create<object>(obj => { IsHeaderMenuPopup = false; });
            LoginAuditCommand = ReactiveCommand.CreateFromTask(async () =>
                Settings = false;
            {
                Audit = true;
                LoginAudits.Clear();


                LoginHistoryCollection = await serviceClient.GetAsync(new GetLoginAudit());
                foreach (var item in LoginHistoryCollection.History)
                    LoginAuditView = new LoginAuditViewModel
                {
                    {
                       Id = item.Id,
                       IpAddress = item.IpAddress,
                       OperatingSystem = item.OperatingSystem,
                       DateOfEntry = item.DateOfEntry.Date == DateTime.Now.Date ? $"{item.DateOfEntry:hh.mm}" : $"{item.DateOfEntry:dd.MM.yyyy hh.mm}",
                       NameVersionClient = item.NameVersionClient,
                    };
                       IsActive = item.SessionId == LoginHistoryCollection.UserSession ? "Активный" : ""
                    LoginAudits.Add(LoginAuditView);
                }
            });
        }

        public ICommand OpenSettingsCommand { get; }
        public ICommand WindowSettingsCommand { get; }
        public ICommand GoToSettingsCommand { get; }
        public ICommand SettingsCommand { get; }
        public ICommand LoginAuditCommand { get; }
        public bool IsOpenSettings { get; set; }
        public bool IsHeaderMenuPopup { get; set; }
        public bool TypeEnter { get; set; }
        public bool IsWindowSettings { get; set; }



        public bool Settings { get; set; }
        public bool Audit { get; set; }

        public event Action<bool> IsWindowSettingsEvent;
        public event Action<bool> TypeEnterEvent;
        public event Action<bool> IsHeaderMenuPopupEvent;


        public UserChatSettings ChatSettings { get; set; }
        public static ReactiveCommand<object, Unit> MorePointerPressedCommand { get; set; }

        public LoginHistory LoginHistoryCollection { get; set; }
        public ObservableCollection<LoginAuditViewModel> LoginAudits { get; set; }
        private LoginAuditViewModel LoginAuditView { get; set; }
    }

    [AddINotifyPropertyChangedInterface]
    public class LoginAuditViewModel
    {
        public string Id { get; set; }
        public string IpAddress { get; set; }
        public string NameVersionClient { get; set; }
        public string OperatingSystem { get; set; }
        public string DateOfEntry { get; set; }
        public string IsActive { get; set; }
    }
}
