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
    public interface IHaveWidth
    {
        double Width { get; }
    }

    [AddINotifyPropertyChangedInterface]
    public class ProfileViewModel
    {
        public ProfileViewModel(IJsonServiceClient serviceClient)
        {
            //Показать/скрыть панель профиля
            SetOpenProfileCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (ProfileViewModel.WindowWidth < 650) IsShowChat = false;
                if (IsOpenProfile && IsEditNameProfile)
                {
                    IsEditNameProfile = false;
                }

                if (IsUserProfileInfo)
                {
                    IsOpenProfile = true;
                    Profile = await serviceClient.GetAsync(new GetMyProfile());
                    IsOpenProfileEvent?.Invoke(IsOpenProfile, IsUserProfileInfo);
                    IsUserProfileInfo = false;
                }
                else
                {
                    Profile = await serviceClient.GetAsync(new GetMyProfile());
                    IsUserProfileInfo = false;
                    IsOpenProfile = !IsOpenProfile;
                    IsOpenProfileEvent?.Invoke(IsOpenProfile, IsUserProfileInfo);
                }
               
            });

            //Сохранить изменения Name профиля
            ApplyProfileNameCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                Profile = await serviceClient.PostAsync(new SetProfile
                    {DisplayName = Profile.DisplayName, AboutMe = Profile.AboutMe });
                IsEditNameProfile = false;
            });

            //Сохранить изменения AboutMe профиля
            ApplyProfileAboutMeCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                Profile = await serviceClient.PostAsync(new SetProfile
                    { AboutMe = Profile.AboutMe, DisplayName = Profile.DisplayName });
                IsEditAboutMeProfile = false;
            });

            //Скрытие окна 
            LayoutUpdatedWindow = ReactiveCommand.Create<object>(obj =>
            {
                if (obj is IHaveWidth window)
                {
                    ProfileViewModel.WindowWidth = window.Width;
                    IsShowChat = !IsOpenProfile || window.Width > 650;
                }
            });

            //Показать/скрыть редактирование профиля
            SetEditNameProfileCommand = ReactiveCommand.CreateFromTask(async () => { IsEditNameProfile = true; });
            SetEditAboutMeProfileCommand = ReactiveCommand.CreateFromTask(async () => { IsEditAboutMeProfile = true; });

            //Показать/скрыть PopupMenuProfile
            PopupMenuProfile = ReactiveCommand.Create<object>(obj => { IsOpenMenu = !IsOpenMenu; });

            SignOutCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                SignOut = !SignOut;
                SignOutEvent?.Invoke(SignOut);
            });

            LoadMessageHistoryCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                LoadMessageHistory = !LoadMessageHistory;
                LoadMessageHistoryEvent?.Invoke(LoadMessageHistory);
            });
        }

        //Profile
        public bool IsOpenProfile { get; set; }
        public bool IsOpenMenu { get; set; }
        public bool IsEditNameProfile { get; set; }
        public bool IsEditAboutMeProfile { get; set; }
        public bool IsShowChat { get; set; } = false;
        public static double WindowWidth { get; set; }
        public bool SignOut { get; set; }
        public bool LoadMessageHistory { get; set; }
        public bool IsUserProfileInfo { get; set; }

        public ICommand ApplyProfileNameCommand { get; }
        public ICommand ApplyProfileAboutMeCommand { get; }
        public ICommand SetOpenProfileCommand { get; }
        public ICommand SetEditNameProfileCommand { get; }
        public ICommand SignOutCommand { get; }
        public ICommand LoadMessageHistoryCommand { get; }
        public ICommand SetEditAboutMeProfileCommand { get; }


        public UserProfileMold Profile { get; set; }

        public event Action<bool,bool> IsOpenProfileEvent;
        public event Action<bool> SignOutEvent;
        public event Action<bool> LoadMessageHistoryEvent;
  

        public ReactiveCommand<object, Unit> LayoutUpdatedWindow { get; }
        public ReactiveCommand<object, Unit> PopupMenuProfile { get; }
    }
}
