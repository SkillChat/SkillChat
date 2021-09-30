using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using PropertyChanged;
using ReactiveUI;
using ServiceStack;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;
using Splat;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class SettingsViewModel
    {
        public SettingsViewModel(IJsonServiceClient serviceClient)
        {
            ChatSettings = new UserChatSettings();

            this.ObservableForProperty(m => m.TypeEnter).Subscribe(change => TypeEnterEvent?.Invoke(change.Value));

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
                SetSelectedMenuItem(SelectedMenuItem.Settings);
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
                SetSelectedMenuItem(SelectedMenuItem.Audit);
                LoginAuditCollection.Clear();
                LoginHistoryCollection = await serviceClient.GetAsync(new GetLoginAudit());
                foreach (var item in LoginHistoryCollection.History.Take(100))
                {
                    LoginAuditView = new LoginAuditViewModel
                    {
                        Id = item.Id,
                        IpAddress = item.IpAddress,
                        OperatingSystem = item.OperatingSystem,
                        NameVersionClient = item.NameVersionClient,
                        DateOfEntry = item.DateOfEntry.Date == DateTime.Now.Date
                            ? $"{item.DateOfEntry:HH:mm}"
                            : $"{item.DateOfEntry:dd.MM.yyyy HH:mm}",
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
        public bool AutoScroll { get; set; }
        public bool IsContextMenu { get; set; }
        public bool TypeEnter { get; set; }
        public bool IsOpened { get; set; }

        public SelectedMenuItem SelectedItem { get; protected set; }

        protected void SetSelectedMenuItem(SelectedMenuItem selected)
        {
            SelectedItem = selected;
            SetSelectedOnSettingsItemEvent?.Invoke(SelectedItem);
        }

        public enum SelectedMenuItem
        {
            Settings,
            Audit
        }

        public void SelectMessage()
        {
            var selectMes = Locator.Current.GetService<SelectMessages>();
            selectMes.IsTurnedSelectMode = true;
            CloseContextMenu();
        }

        public bool SettingsMenuActiveMain => SelectedItem == SelectedMenuItem.Settings;
        public bool AuditMenuActiveMain => SelectedItem == SelectedMenuItem.Audit;

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
