using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using PropertyChanged;
using ReactiveUI;
using ServiceStack;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;
using Splat;

namespace SkillChat.Client.ViewModel
{
    public interface IHaveWidth
    {
        double Width { get; }
    }

    [AddINotifyPropertyChangedInterface]
    public class ProfileViewModel
    {
        private readonly IJsonServiceClient _serviceClient;

        public ProfileViewModel(IJsonServiceClient serviceClient)
        {
            _serviceClient = serviceClient;
            //Показать/скрыть панель профиля
            SetOpenProfileCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (ProfileViewModel.WindowWidth < 650) IsShowChat = false;
                if (IsOpened && IsEditNameProfile)
                {
                    IsEditNameProfile = false;
                }

                if (IsUserProfileInfo)
                {
                    IsOpened = true;
                    Profile = await _serviceClient.GetAsync(new GetMyProfile());
                    IsOpenProfileEvent?.Invoke(IsOpened, IsUserProfileInfo);
                    IsUserProfileInfo = false;
                }
                else
                {
                    if (IsOpened)
                    {
                        Close();
                    }
                    else
                    {
                        IsOpened = true;
                    }
                    Profile = await _serviceClient.GetAsync(new GetMyProfile());
                    IsOpenProfileEvent?.Invoke(IsOpened, IsUserProfileInfo);
                    IsUserProfileInfo = false;
                }
               
            });

            //Сохранить изменения Name профиля
            ApplyProfileNameCommand = ReactiveCommand.CreateFromTask(async () =>  await SetProfile() );

            //Сохранить изменения AboutMe профиля
            ApplyProfileAboutMeCommand = ReactiveCommand.CreateFromTask(async () => await SetProfile() );

            //Скрытие окна 
            LayoutUpdatedWindow = ReactiveCommand.Create<object>(obj =>
            {
                if (obj is IHaveWidth window)
                {
                    ProfileViewModel.WindowWidth = window.Width;
                    IsShowChat = !IsOpened || window.Width > 650;
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
        
        private async Task SetProfile()
        {
            Profile = await _serviceClient.PostAsync(new SetProfile
            {
                AboutMe = Profile.AboutMe,
                DisplayName = Profile.DisplayName
            });
            IsEditNameProfile = false;
        }

        //Profile
        public bool IsOpened { get; protected set; }
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

        public void Close()
        {
            IsOpened = false;
            Profile = null;
        }

        public async Task Open(string userId)
        {
            var profile = await _serviceClient.GetAsync(new GetProfile { UserId = userId });
            var lastProfile = Profile;
            Profile = profile;
            var user = Locator.Current.GetService<CurrentUserViewModel>();
            if (user.Id == profile.Id)
            {
                IsOpened = !IsOpened;
                IsUserProfileInfo = true;
            }
            else
            {
                if (lastProfile?.Id == profile.Id)
                {
                    Close();
                }
                else
                {
                    IsOpened = true;
                    IsUserProfileInfo = true;
                }
            }
        }
    }
}
