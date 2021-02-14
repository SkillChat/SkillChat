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

            LoginAuditCollection = new ObservableCollection<LoginAuditViewModel>();

            ContextMenuCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                IsContextMenu = !IsContextMenu;
                ContextMenuSettingsActiveEvent?.Invoke(IsContextMenu);
            });

            OpenSettingsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                IsOpened = !IsOpened;
                CloseContextMenu();
                GetSettingsCommand.Execute(null);
                OpenSettingsActiveEvent?.Invoke(IsOpened);
            });

            GetSettingsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var settingsUser = await serviceClient.GetAsync(new GetMySettings());
                if (settingsUser.SendingMessageByEnterKey)
                {
                    TypeEnter = settingsUser.SendingMessageByEnterKey;
                    TypeEnterEvent?.Invoke(TypeEnter);
                }
            });

            SaveSettingsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var settingsUser = await serviceClient.PostAsync(new SetSettings {SendingMessageByEnterKey = TypeEnter});
                ChatSettings = settingsUser;
            });

            MorePointerPressedCommand = ReactiveCommand.Create<object>(obj => { IsContextMenu = false; });

            GetHistoryLoginAuditCommand = ReactiveCommand.CreateFromTask(async () =>
            {

                LoginAuditCollection.Clear();
                LoginHistoryCollection = await serviceClient.GetAsync(new GetLoginAudit());
                foreach (var item in LoginHistoryCollection.History)
                {
                    LoginAuditView = new LoginAuditViewModel
                    {
                        Id = item.Id,
                        IpAddress = item.IpAddress,
                        OperatingSystem = item.OperatingSystem,
                        NameVersionClient = item.NameVersionClient,
                        DateOfEntry = item.DateOfEntry.Date == DateTime.Now.Date ? $"{item.DateOfEntry:HH:mm}" : $"{item.DateOfEntry:dd.MM.yyyy HH:mm}",
                        IsActive = item.SessionId == LoginHistoryCollection.UniqueSessionUser ? "Активный" : ""
                    };
                    LoginAuditCollection.Add(LoginAuditView);
                }
            });
        }

        public void Close()
        {
            IsOpened = false;
        }
        public void CloseContextMenu()
        {
            IsContextMenu = false;
        }

        public ICommand ContextMenuCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand GetSettingsCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand GetHistoryLoginAuditCommand { get; }
        public bool IsContextMenu { get; set; }
        public bool TypeEnter { get; set; }
        public bool IsOpened { get; set; }



        public bool Settings { get; set; }
        public bool Audit { get; set; }

        public event Action<bool> OpenSettingsActiveEvent;
        public event Action<bool> TypeEnterEvent;
        public event Action<bool> ContextMenuSettingsActiveEvent;
        public event Action<Enum> SetSelectedOnSettingsItemEvent;


        public UserChatSettings ChatSettings { get; set; }
        public static ReactiveCommand<object, Unit> MorePointerPressedCommand { get; set; }

        public LoginHistory LoginHistoryCollection { get; set; }
        public ObservableCollection<LoginAuditViewModel> LoginAuditCollection { get; set; }
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
