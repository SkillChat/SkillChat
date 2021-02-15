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
    public class ProfileViewModel: IProfile
    {
        private readonly IJsonServiceClient _serviceClient;

        public ProfileViewModel(IJsonServiceClient serviceClient)
        {
            _serviceClient = serviceClient;
            //Показать/скрыть панель профиля
            OpenProfilePanelCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var user = Locator.Current.GetService<CurrentUserViewModel>();
                await Open(user.Id);
            });

            //Сохранить изменения Name профиля
            ApplyProfileNameCommand = ReactiveCommand.CreateFromTask(async () => await SetProfile());

            //Сохранить изменения AboutMe профиля
            ApplyProfileAboutMeCommand = ReactiveCommand.CreateFromTask(async () => await SetProfile());

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
            SetEditNameProfileCommand = ReactiveCommand.Create(() => ResetEditMode(UserProfileEditMode.DisplayName));
            SetEditAboutMeProfileCommand = ReactiveCommand.Create(() => ResetEditMode(UserProfileEditMode.AboutMe));

            //Показать/скрыть ContextMenu
            ContextMenuProfile = ReactiveCommand.Create<object>(obj => { IsActiveContextMenu = !IsActiveContextMenu; });

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
            ResetEditMode();
        }

        //Profile
        /// <summary>
        /// Панель профиля открыта
        /// </summary>
        public bool IsOpened { get; protected set; }

        public bool IsActiveContextMenu { get; set; }

        /// <summary>
        /// Режим редактирования имени
        /// </summary>
        public bool IsEditNameProfile => EditMode == UserProfileEditMode.DisplayName;

        /// <summary>
        /// Режим редактирования о себе
        /// </summary>
        public bool IsEditAboutMeProfile => EditMode == UserProfileEditMode.AboutMe;

        public UserProfileEditMode EditMode { get; protected set; }

        protected void ResetEditMode(UserProfileEditMode mode = UserProfileEditMode.None)
        {
            EditMode = mode;
        }

        public enum UserProfileEditMode
        {
            None,
            DisplayName,
            AboutMe
        }

        public bool IsShowChat { get; protected set; } = false;
        public static double WindowWidth { get; set; }
        public bool SignOut { get; set; }
        public bool LoadMessageHistory { get; set; }
        public bool IsMyProfile => Locator.Current.GetService<CurrentUserViewModel>()?.Id == Profile?.Id;

        public ICommand ApplyProfileNameCommand { get; }
        public ICommand ApplyProfileAboutMeCommand { get; }
        public ICommand OpenProfilePanelCommand { get; }
        public ICommand SetEditNameProfileCommand { get; }
        public ICommand SignOutCommand { get; }
        public ICommand LoadMessageHistoryCommand { get; }
        public ICommand SetEditAboutMeProfileCommand { get; }

        public UserProfileMold Profile { get; protected set; }

        public event Action IsOpenProfileEvent;
        public event Action<bool> SignOutEvent;
        public event Action<bool> LoadMessageHistoryEvent;


        public ReactiveCommand<object, Unit> LayoutUpdatedWindow { get; }
        public ReactiveCommand<object, Unit> ContextMenuProfile { get; }

        public void Close()
        {
            IsOpened = false;
            Profile = null;
        }

        public void ContextMenuClose()
        {
            IsActiveContextMenu = false;
        }

        public async Task Open(string userId)
        {
            var lastProfileId = Profile?.Id;
            if (lastProfileId == userId)
            {
                if (IsOpened) Close();
            }
            else
            {
                ResetEditMode();
                Profile = await _serviceClient.GetAsync(new GetProfile { UserId = userId });
                IsOpened = true;
                IsOpenProfileEvent?.Invoke();
            }
        }
    }
}
