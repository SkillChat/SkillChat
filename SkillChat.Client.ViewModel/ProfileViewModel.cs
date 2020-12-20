using System;
using System.Collections.Generic;
using System.Reactive;
using System.Windows.Input;
using PropertyChanged;
using ReactiveUI;
using ServiceStack;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;

namespace SkillChat.Client.ViewModel
{
    public interface IHaveWidth
    {
        double Width { get; }
    }

    [AddINotifyPropertyChangedInterface]
    public class ProfileViewModel
    {
        public ProfileViewModel(IJsonServiceClient serviceClient, MainWindowViewModel mainWindow)
        {
            WindowViewModel = mainWindow;
            //Показать/скрыть панель профиля
            SetOpenProfileCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (ProfileViewModel.windowWidth < 650) isShowChat = false;
                if (isOpenProfile && isEditProfile)
                {
                    isEditProfile = false;
                }
                isOpenProfile = !isOpenProfile;
                WindowViewModel.SettingsViewModel.IsWindowSettings = false;
            });
            //Показать/скрыть редактирование профиля
            SetEditProfileCommand = ReactiveCommand.CreateFromTask(async () => { isEditProfile = true; });
            //Сохранить изменения профиля
            ApplyProfileCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    Profile = await serviceClient.PostAsync(new SetProfile { DisplayName = Profile.DisplayName });
                    isEditProfile = false;
                });
            //Скрытие окна 
            LayoutUpdatedWindow = ReactiveCommand.Create<object>(obj =>
            {
                if (obj is IHaveWidth window)
                {
                    ProfileViewModel.windowWidth = window.Width;
                    isShowChat = !isOpenProfile || window.Width > 650;
                }
            });
        }

        //Profile
        public bool isOpenProfile { get; set; }
        public bool isEditProfile { get; set; }
        
        public bool isShowChat { get; set; } = false;
        public static double windowWidth { get; set; }

        public UserProfileMold Profile { get; set; }

        public MainWindowViewModel WindowViewModel { get; set; }

        public ICommand ApplyProfileCommand { get; }

        public ICommand SetOpenProfileCommand { get; }

        public ICommand SetEditProfileCommand { get; }

        public ReactiveCommand<object, Unit> LayoutUpdatedWindow { get; }
    }
}
