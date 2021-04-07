using PropertyChanged;
using ReactiveUI;
using ServiceStack;
using SkillChat.Interface;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;
using Splat;
using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SkillChat.Client.ViewModel
{
    public interface IHaveWidth
    {
        double Width { get; }
    }

    [AddINotifyPropertyChangedInterface]
    public class ProfileViewModel : IProfile
    {
        private readonly IJsonServiceClient _serviceClient;
        private IChatHub _hub;

        public ProfileViewModel(IJsonServiceClient serviceClient)
        {
            _serviceClient = serviceClient;

            //Показать/скрыть панель профиля
            OpenProfilePanelCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var user = Locator.Current.GetService<ICurrentUser>();
                await Open(user?.Id);
            });

            //Сохранить изменения Name профиля
            ApplyProfileNameCommand = ReactiveCommand.CreateFromTask(SetProfile);

            //Сохранить изменения AboutMe профиля
            ApplyProfileAboutMeCommand = ReactiveCommand.CreateFromTask(SetProfile);

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
        }

        private async Task SetProfile()
        {
            UpdateProfileProps(await _serviceClient.PostAsync(new SetProfile
            {
                AboutMe = AboutMe,
                DisplayName = DisplayName
            }));

            await _hub.UpdateMyDisplayName(DisplayName);

            ResetEditMode();
        }

        public void SetChatHub(IChatHub chatHub) => _hub = chatHub;

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
        public bool IsMyProfile => Locator.Current.GetService<ICurrentUser>()?.Id == ProfileId;

        public ICommand ApplyProfileNameCommand { get; }
        public ICommand ApplyProfileAboutMeCommand { get; }
        public ICommand OpenProfilePanelCommand { get; }
        public ICommand SetEditNameProfileCommand { get; }
        public ICommand SignOutCommand { get; set; }
        public ICommand LoadMessageHistoryCommand { get; set; }
        public ICommand SetEditAboutMeProfileCommand { get; }

        public string ProfileId { get; set; }
        public string Login { get; set; }
        public string DisplayName { get; set; }
        public string AboutMe { get; set; }

        public event Action IsOpenProfileEvent;

        public ReactiveCommand<object, Unit> LayoutUpdatedWindow { get; }
        public ReactiveCommand<object, Unit> ContextMenuProfile { get; }

        public void Close()
        {
            IsOpened = false;

            ProfileId = string.Empty;
            Login = string.Empty;
            DisplayName = string.Empty;
            AboutMe = string.Empty;
        }

        public void ContextMenuClose()
        {
            IsActiveContextMenu = false;
        }

        public void UpdateUserProfile(string newUserDisplayName, string userId)
        {
            if (ProfileId == userId)
            {
                DisplayName = newUserDisplayName;
            }
        }

        public async Task Open(string userId)
        {
            var lastProfileId = ProfileId;
            if (lastProfileId == userId)
            {
                if (IsOpened) Close();
            }
            else
            {
                ResetEditMode();
                UpdateProfileProps(await _serviceClient.GetAsync(new GetProfile { UserId = userId }));

                IsOpened = true;
                IsOpenProfileEvent?.Invoke();
            }
        }

        private void UpdateProfileProps(UserProfileMold profile)
        {
            ProfileId = profile.Id;
            Login = profile.Login;
            DisplayName = profile.DisplayName;
            AboutMe = profile.AboutMe;
        }
    }
}
