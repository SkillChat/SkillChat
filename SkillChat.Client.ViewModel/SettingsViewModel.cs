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
        public SettingsViewModel(IJsonServiceClient serviceClient)
        {
            ChatSettings = new UserChatSettings();
            OpenSettingsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                IsHeaderMenuPopup = !IsHeaderMenuPopup;
                IsHeaderMenuPopupEvent?.Invoke(IsHeaderMenuPopup);
            });

            GoToSettingsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                IsHeaderMenuPopup = false;
                IsWindowSettings = !IsWindowSettings;
                IsWindowSettingsEvent?.Invoke(IsWindowSettings);
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
        }

        public ICommand OpenSettingsCommand { get; }
        public ICommand GoToSettingsCommand { get; }
        public ICommand SettingsCommand { get; }


        public bool IsHeaderMenuPopup { get; set; }
        public bool TypeEnter { get; set; }
        public bool IsWindowSettings { get; set; }


        public event Action<bool> IsWindowSettingsEvent;
        public event Action<bool> TypeEnterEvent;
        public event Action<bool> IsHeaderMenuPopupEvent;


        public UserChatSettings ChatSettings { get; set; }
        public static ReactiveCommand<object, Unit> MorePointerPressedCommand { get; set; }
    }
}