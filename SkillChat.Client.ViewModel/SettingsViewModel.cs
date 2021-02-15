using System;
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
        public SettingsViewModel(IJsonServiceClient serviceClient, MainWindowViewModel mainWindow)
        {
            ChatSettings = new UserChatSettings();
            MainWindowViewModel = mainWindow;

            OpenSettingsCommand = ReactiveCommand.CreateFromTask(async () => { IsOpenSettings = !IsOpenSettings; });

            GoToSettingsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                IsOpenSettings = false;
                IsWindowSettings = !IsWindowSettings;
                var settings = await serviceClient.GetAsync(new GetMySettings());

                if (settings.SendingMessageByEnterKey)
                    TypeEnter = settings.SendingMessageByEnterKey;
            });

            SettingsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var settings = await serviceClient.PostAsync(new SetSettings {SendingMessageByEnterKey = TypeEnter  });
                ChatSettings = settings;
            });

            MorePointerPressedCommand = ReactiveCommand.Create<object>(obj =>
            {
                IsOpenSettings = false;
            });


        }

        public ICommand OpenSettingsCommand { get; }
        public ICommand GoToSettingsCommand { get; }
        public ICommand SettingsCommand { get; }
        
        public bool IsOpenSettings { get; set; }

        private bool _typeEnter;
        public bool TypeEnter
        {
            get => _typeEnter;
            set
            {
                _typeEnter = value;
                TypeEnterEvent?.Invoke(value);
            }
            
        }

        private bool _isWindowSettings;
        public bool IsWindowSettings
        {
            get => _isWindowSettings;
            set
            {
                _isWindowSettings = value;
                IsWindowSettingsEvent?.Invoke(value);
            }
        }

        public event Action<bool> IsWindowSettingsEvent;
        public event Action<bool> TypeEnterEvent;

        public UserChatSettings ChatSettings { get; set; }
        public ProfileViewModel ProfileViewModel { get; set; }
        public MainWindowViewModel MainWindowViewModel { get; set; }
        public static ReactiveCommand<object, Unit> MorePointerPressedCommand { get; set; }
    }
}