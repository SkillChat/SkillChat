using AutoMapper;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using PropertyChanged;
using ReactiveUI;
using ServiceStack;
using SignalR.EasyUse.Client;
using SkillChat.Interface;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.ServiceModel.Molds.Attachment;
using SkillChat.Client.ViewModel.Services;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using SkillChat.Client.ViewModel;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class MainWindowViewModel
    {
        IConfiguration configuration;
        ChatClientSettings settings;

        public MainWindowViewModel()
        {
            Locator.CurrentMutable.RegisterConstant(this);
            User = new CurrentUserViewModel();
            RegisterUser = new RegisterUserViewModel();
            SelectMessagesMode = new SelectMessages();
            Locator.CurrentMutable.RegisterConstant<SelectMessages>(SelectMessagesMode);
            Locator.CurrentMutable.RegisterConstant<ICurrentUser>(User);
            configuration = Locator.Current.GetService<IConfiguration>();
            settings = configuration?.GetSection("ChatClientSettings")?.Get<ChatClientSettings>();

            var mapper = Locator.Current.GetService<IMapper>();

            if (settings == null)
            {
                settings = new ChatClientSettings();
            }

            if (settings.HostUrl.IsNullOrEmpty())
            {
                settings.HostUrl = "http://localhost:5000";
            }

            if (settings.AttachmentDefaultPath.IsNullOrEmpty())
            {
                settings.AttachmentDefaultPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                    "Download");
            }

            serviceClient = Locator.Current.GetService<ISkillChatApiClient>()
                ?? new ServiceStackSkillChatApiClient(settings.HostUrl);
            Locator.CurrentMutable.RegisterConstant(new AttachmentManager(settings.AttachmentDefaultPath, serviceClient));

            ProfileViewModel = new ProfileViewModel(serviceClient);
            Locator.CurrentMutable.RegisterConstant<IProfile>(ProfileViewModel);
            ProfileViewModel.IsOpenProfileEvent += () => WindowStates(WindowState.OpenProfile);
            ((INotifyPropertyChanged)ProfileViewModel).PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ProfileViewModel.IsOpened))
                {
                    SyncSidebarSelection();
                }
            };

            AttachmentViewModel = new SendAttachmentsViewModel(serviceClient);
            ConfirmationViewModel = new ConfirmationViewModel();

            SettingsViewModel = new SettingsViewModel(serviceClient);
            SettingsViewModel.OpenSettingsActiveEvent += (e) => { WindowStates(WindowState.WindowSettings); };
            SettingsViewModel.TypeEnterEvent += (e) => { KeySendMessage = e; };
            SettingsViewModel.ContextMenuSettingsActiveEvent += (e) => { WindowStates(WindowState.HeaderMenuPopup); };
            SettingsViewModel.SetSelectedOnSettingsItemEvent += e => { TextHeaderMenuInSettings = SettingsViewModel.SettingsMenuActiveMain ? "Сообщения и чаты" : "Аудит входа"; };
            ((INotifyPropertyChanged)SettingsViewModel).PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SettingsViewModel.IsOpened))
                {
                    SyncSidebarSelection();
                }
            };

            Width(false);
            SyncSidebarSelection();
            User.Login = settings.Login;
            
            Tokens = new TokenResult { AccessToken = settings.AccessToken, RefreshToken = settings.RefreshToken };

            Messages = new ObservableCollection<MessageViewModel>();

            ConnectCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var clientContext = ResolveClientContext();

                try
                {
                    _connection = new HubConnectionBuilder()
                        .WithUrl(settings.HostUrl + "/ChatHub")
                        .Build();

                    _hub = _connection.CreateHub<IChatHub>();
                    ProfileViewModel.SetChatHub(_hub);
                    AttachmentViewModel.SetChatHub(_hub);

                    if (Tokens == null || Tokens.AccessToken.IsNullOrEmpty())
                    {
                        Tokens = await serviceClient.AuthenticateAsync(new AuthViaPassword
                        { Login = User.Login, Password = User.Password });

                        settings.AccessToken = Tokens.AccessToken;
                        settings.RefreshToken = Tokens.RefreshToken;
                        settings.UserName = User.UserName;
                        settings.Login = User.Login;
                        configuration.GetSection("ChatClientSettings").Set(settings);
                    }

                    serviceClient.BearerToken = Tokens.AccessToken;
                    this.ObservableForProperty(m => m.ExpireTime).Subscribe(change =>
                    {
                        if (change.Value != null)
                        {
                            //TODO запуск обновления токена
                        }
                    });

                    _connection.Subscribe<LogOn>(async data =>
                    {
                        LogOn.LogOnStatus error = data.Error;
                        switch (error)
                        {
                            case LogOn.LogOnStatus.ErrorUserNotFound: //Пользователь не найден
                                {
                                    try
                                    {
                                        SignOutCommand.Execute(null);
                                        IsShowingLoginPage = false;
                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                    finally
                                    {
                                        RegisterUser.ErrorMessageRegisterPage.GetErrorMessage("Пользователь не найден");
                                        RegisterUser.Login = settings.Login;
                                        IsShowingRegisterPage = true;
                                    }
                                    break;
                                }
                            case LogOn.LogOnStatus.ErrorExpiredToken: //Срок действия токена истек
                                {
                                    serviceClient.BearerToken = Tokens.RefreshToken;
                                    try
                                    {
                                        Tokens = await serviceClient.RefreshTokenAsync(new PostRefreshToken());
                                        settings.AccessToken = Tokens.AccessToken;
                                        settings.RefreshToken = Tokens.RefreshToken;
                                        configuration.GetSection("ChatClientSettings").Set(configuration);
                                        await _hub.Login(
                                            Tokens.AccessToken,
                                            clientContext.OperatingSystem,
                                            clientContext.IpAddress,
                                            clientContext.ClientName);
                                        IsSignedIn = true;
                                    }
                                    catch (Exception e)
                                    {
                                        User.ErrorMessageLoginPage.GetErrorMessage("419");
                                        SignOutCommand.Execute(null);
                                    }
                                    break;
                                }
                            case LogOn.LogOnStatus.Ok: //Автовход по токену
                                {
                                    User.Id = data.Id;
                                    User.Login = data.UserLogin;
                                    User.UserName = data.UserName;
                                    ExpireTime = data.ExpireTime;
                                    var chats = await serviceClient.GetChatsAsync(new GetChatsList());
                                    var chat = chats.Chats.FirstOrDefault();
                                    ChatId = chat?.Id;
                                    ChatName = chat?.ChatName;
                                    LoadMessageHistoryCommand.Execute(null);
                                    //Получаем настройки
                                    SettingsViewModel.ChatSettings = await serviceClient.GetMySettingsAsync(new GetMySettings());
                                    KeySendMessage = SettingsViewModel.ChatSettings.SendingMessageByEnterKey;
                                    IsSignedIn = true;
                                    break;
                                }
                            default:
                                {
                                    User.ErrorMessageLoginPage.GetErrorMessage(((int)error).ToString());
                                    break;
                                }
                        }

                    });

                    _connection.Subscribe<UpdateUserDisplayName>(async user =>
                    {
                        try
                        {
                            foreach (var item in Messages)
                            {
                                if (item.UserId == user.Id)
                                {
                                    if (item.ShowNickname) item.UserNickname = 
                                        Helpers.Helpers.NameOrLogin(user.DisplayName, user.UserLogin);
                                }
                            }
                            ProfileViewModel.UpdateUserProfile(user.DisplayName, user.Id);
                            if (user.Id == User.Id) User.UserName = user.DisplayName;
                        }
                        catch (Exception e)
                        {
                            SignOutCommand.Execute(null);
                        }
                    });

                    ///Обновление отредактированных сообщений в окне чата.
                    _connection.Subscribe<ReceiveEditedMessage>(async data =>
                    {
                        if (messageDictionary.TryGetValue(data.Id, out var keyValue))
                        {
                            keyValue.Text = data.Text;
                            keyValue.LastEditTime = data.LastEditTime;

                            if (data.QuotedMessage != null)
                            {
                                MessageViewModel newQuotedMessage = mapper.Map<MessageViewModel>(data.QuotedMessage);
                                if (messageDictionary.TryGetValue(data.QuotedMessage.Id, out var message))
                                {
                                    mapper.Map(newQuotedMessage, message);
                                    keyValue.QuotedMessage = message;
                                }
                                else
                                {
                                    newQuotedMessage.IsMyMessage = User.Id == data.QuotedMessage.UserId;
                                    newQuotedMessage.Attachments = data.QuotedMessage.Attachments?
                                                                            .Select(s =>
                                                                            {
                                                                                var attah = mapper?.Map<AttachmentMold>(s);
                                                                                var newAttachment = new AttachmentMessageViewModel(attah);
                                                                                return newAttachment;
                                                                            }).ToList();

                                    messageDictionary[data.QuotedMessage.Id] = newQuotedMessage;
                                    keyValue.QuotedMessage = newQuotedMessage;
                                }
                            }
                            else
                            {
                                keyValue.QuotedMessage = null;
                            }
                        }
                    });

                    ///Получает новые сообщения и добавляет их в окно чата. 
                    _connection.Subscribe<ReceiveMessage>(async data =>
                    {
                        MessageViewModel newMessage = mapper.Map<MessageViewModel>(data);

                        newMessage.IsMyMessage = User.Id == data.UserId;
                        newMessage.Attachments = data.Attachments?
                            .Select(s =>
                            {
                                var attah = mapper?.Map<AttachmentMold>(s);
                                var newAttachment = new AttachmentMessageViewModel(attah);
                                return newAttachment;
                            }).ToList();
                        if (data.QuotedMessage != null)
                        {
                            MessageViewModel newQuotedMessage = mapper.Map<MessageViewModel>(data.QuotedMessage);
                            if (messageDictionary.TryGetValue(data.QuotedMessage.Id, out var quotedMessage))
                            {
                                mapper.Map(newQuotedMessage, quotedMessage);
                                newMessage.QuotedMessage = quotedMessage;
                            }
                            else
                            {
                                newQuotedMessage.IsMyMessage = User.Id == data.QuotedMessage.UserId;
                                newQuotedMessage.Attachments = data.QuotedMessage.Attachments?
                                    .Select(s =>
                                    {
                                        var attah = mapper?.Map<AttachmentMold>(s);
                                        var newAttachment = new AttachmentMessageViewModel(attah);
                                        return newAttachment;
                                    }).ToList();

                                messageDictionary[newQuotedMessage.Id] = newQuotedMessage;
                            }
                        }
                        if (Messages.Count != 0)
                        {
                            if (Messages.Last().UserId != data.UserId)
                            {
                                newMessage.ShowNickname = true;
                            }
                        }
                        else
                        {
                            newMessage.ShowNickname = true;
                        }
                        Messages.Add(newMessage);
                        MessageReceived?.Invoke(new ReceivedMessageArgs(newMessage));
                        messageDictionary[newMessage.Id] = newMessage;
                        AutoScrollWhenSendingMyMessage = User.Id == newMessage.UserId;
                    });

                    _connection.Closed += connectionOnClosed();
                    await _connection.StartAsync();
                    await _hub.Login(
                        Tokens.AccessToken,
                        clientContext.OperatingSystem,
                        clientContext.IpAddress,
                        clientContext.ClientName);
                    IsShowingLoginPage = false;
                    IsShowingRegisterPage = false;
                    User.ErrorMessageLoginPage.ResetDisplayErrorMessage();
                    IsConnected = _connection.State == HubConnectionState.Connected;
                    User.Password = "";
                }
                catch (Exception e)
                {
                    User.ErrorMessageLoginPage.GetErrorMessage(e.ToStatusCode().ToString());
                    User.ErrorMessageLoginPage.IsError = true;
                    IsShowingLoginPage = _connection.State != HubConnectionState.Connected;
                }
            });

            if (Tokens.AccessToken.IsNullOrEmpty() == false)
            {
                ConnectCommand.Execute(null);
            }

            //Команда выбирает последнее отправленное сообщение пользователя 
            EditLastMessageCommand = ReactiveCommand.Create(EditLastMessageMethod);

            //Выход из режима редактирования (Escape)
            EndEditCommand = ReactiveCommand.Create(EndEditMethod);

            //Команда для отправки сообщения 
            SendCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    try
                    {
                        var IdQuotedMes = SelectedQuotedMessage == null ? "" : SelectedQuotedMessage.Id;
                        MessageText = MessageText.Trim(); //Удаление пробелов в начале и конце сообщения
                        if (MessageText != string.Empty) //Проверка на пустое сообщение
                        {
                            if (idEditMessage != null)
                            {
                                if (IdQuotedMes != idEditMessage)
                                {
                                    await _hub.UpdateMessage(new HubEditedMessage(idEditMessage, ChatId, MessageText, IdQuotedMes)); ///Отправка отредактированного сообщения
                                }
                                idEditMessage = null;
                                CancelQuoted();
                            }
                            else
                            {
                                await _hub.SendMessage(new HubMessage(ChatId, MessageText, IdQuotedMes));
                                CancelQuoted();
                            }

                            MessageText = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        SignOutCommand.Execute(null);
                    }
                },
                this.WhenAnyValue(m => m.IsConnected, m => m.MessageText,
                    (b, m) => b == true && !string.IsNullOrEmpty(m)));

            LoadMessageHistoryCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                try
                {
                    var isInitialLoad = Messages.Count == 0;
                    if (isInitialLoad)
                    {
                        messageDictionary.Clear();
                        ResetUnreadBoundaryState();
                    }

                    var result = await LoadMessageHistoryPageAsync(mapper, Messages?.FirstOrDefault()?.PostTime);

                    if (isInitialLoad && !result.FirstUnreadMessageId.IsNullOrEmpty())
                    {
                        await EnsureUnreadBoundaryMessageIsLoadedAsync(mapper, result.FirstUnreadMessageId, result.HasMoreBefore);

                        var unreadMessage = Messages.FirstOrDefault(message =>
                            string.Equals(message.Id, result.FirstUnreadMessageId, StringComparison.Ordinal));
                        if (unreadMessage != null)
                        {
                            unreadMessage.IsUnreadBoundary = true;
                            InitialUnreadBoundaryMessageId = unreadMessage.Id;
                        }
                    }
                }
                catch (Exception e)
                {
                }
            });

            SignOutCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                try
                {
                    messageDictionary.Clear();
                    EndEditCommand.Execute(null); 
                    ResetUnreadBoundaryState();
                    Messages.Clear();
                    MessageText = null;
                    Tokens = null;
                    IsSignedIn = false;
                    IsConnected = false;
                    serviceClient.BearerToken = null;
                    if (_connection != null)
                    {
                        _connection.Closed -= connectionOnClosed();
                        await _connection.StopAsync();
                    }

                    _connection = null;
                    _hub = null;

                    settings.AccessToken = null;
                    settings.RefreshToken = null;
                    configuration.GetSection("ChatClientSettings").Set(settings);

                    WindowStates(WindowState.SignOut);
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    IsShowingLoginPage = true;
                }
            });

            IsShowingLoginPage = true;
            IsShowingRegisterPage = false;

            GoToRegisterCommand = ReactiveCommand.Create<object>(_ =>
            {
                RegisterUser.ErrorMessageRegisterPage.ResetDisplayErrorMessage();
                RegisterUser.ErrorMessageRegisterPage.IsError = false;
                IsShowingRegisterPage = true;
                IsShowingLoginPage = false;
                User.Password = "";
            });


            RegisterUser.GoToLoginCommand = ReactiveCommand.Create<object>(_ =>
            {
                User.ErrorMessageLoginPage.ResetDisplayErrorMessage();
                User.ErrorMessageLoginPage.IsError = false;
                IsShowingRegisterPage = false;
                IsShowingLoginPage = true;
                RegisterUser.Password = "";
            });
            IsConnected = false; //Скрывает окно чата            
            RegisterCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var request = new RegisterNewUser();
                try
                {
                    IsSignedIn = true;
                    RegisterUser.ErrorMessageRegisterPage.ResetDisplayErrorMessage();
                    if (string.IsNullOrWhiteSpace(RegisterUser.Login) || string.IsNullOrWhiteSpace(RegisterUser.Password))
                    {
                        RegisterUser.ErrorMessageRegisterPage.GetErrorMessage("Не заполнены Логин и/или Пароль");
                        RegisterUser.ErrorMessageRegisterPage.IsError = true;
                        return;
                    }
                    request.Login = RegisterUser.Login;
                    request.Password = RegisterUser.Password;
                    request.UserName = RegisterUser.UserName;

                    Tokens = await serviceClient.RegisterAsync(request);

                    settings.AccessToken = Tokens.AccessToken;
                    settings.RefreshToken = Tokens.RefreshToken;
                    settings.UserName = RegisterUser.UserName;
                    settings.Login = RegisterUser.Login;
                    configuration.GetSection("ChatClientSettings").Set(settings);

                    //Очистка полей регистрации
                    RegisterUser.Clear(); ;

                    ConnectCommand.Execute(null);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Ошибка регистрации {e.Message}");

                    RegisterUser.ErrorMessageRegisterPage.GetErrorMessage(e.ToStatusCode().ToString());
                    RegisterUser.ErrorMessageRegisterPage.IsError = true;
                }
            });

            NotifyCommand = ReactiveCommand.Create<object>(obj =>
            {
                if (obj is IHaveIsActive win)
                {
                    windowIsFocused = win.IsActive;
                }
            });
            PointerPressedCommand = ReactiveCommand.Create<object>(obj =>
            {
                ProfileViewModel.ContextMenuClose();
                SettingsViewModel.CloseContextMenu();
            });

            // Команда очищает всю историю чата для текущего пользователя.
            ICommand cleaningAllCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                messageDictionary.Clear();
                ResetUnreadBoundaryState();
                Messages.Clear();
                await _hub.CleanChatForMe(ChatId);

                EndEditCommand.Execute(null);
                CancelQuoted();
                SelectMessagesMode.CheckOff();
                ConfirmationViewModel.Close();
            });

            // Команда удаляет выбранные сообщения для текущего пользователя.
            ICommand deleteMessagesCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                List<string> idDeleteMessages = new List<string>();
                foreach (var item in SelectMessagesMode.SelectedCollection)
                {
                    idDeleteMessages.Add(item.Id);
                    Messages.Remove(item);
                }
                await _hub.DeleteMessagesForMe(idDeleteMessages);

                EndEditCommand.Execute(null);
                CancelQuoted();
                SelectMessagesMode.CheckOff();
                ConfirmationViewModel.Close();
            });


            MessageCleaningCommand = ReactiveCommand.Create(() =>
            {
                SettingsViewModel.CloseContextMenu();
                SettingsViewModel.IsOpened = false;
                ConfirmationViewModel.Open(cleaningAllCommand, "Очистить у себя всю историю чата?", "Очистить");
            });

            SelectedMessagesDeleteCommand = ReactiveCommand.Create(() =>
            {
                ConfirmationViewModel.Open(deleteMessagesCommand, "Удалить у себя выбранные сообщения?", "Удалить");
            });





            ProfileViewModel.SignOutCommand = SignOutCommand;
            ProfileViewModel.LoadMessageHistoryCommand = LoadMessageHistoryCommand;

            ToggleSidebarCommand = ReactiveCommand.Create(ToggleSidebar);
            ShowChatsCommand = ReactiveCommand.Create(ShowChats);

            this.WhenAnyValue(model => model.IsSignedIn,model => model.User.DisplayName,model =>
                model.ChatName,(signedIn, displayName, chatName) =>
                IsSignedIn ? $"SkillChat - {User.DisplayName} [{ChatName}]" : $"SkillChat").Subscribe(s => Title = s);
        }

        /// <summary>
        /// Метод выхода из режима редактирования
        /// </summary>
        private void EndEditMethod()
        {
            if (idEditMessage != null)
            {
                if (idEditMessage != null && messageDictionary.TryGetValue(idEditMessage, out var editedMessage))
                {
                    editedMessage.Selected = false;
                }
                EditMessage(null);
            }
        }

        /// <summary>
        /// Метод для выбора последнего сообщения от текущего
        /// пользователя и переход в режим редактирования
        /// </summary>
        private void EditLastMessageMethod()
        {
            if (string.IsNullOrEmpty(MessageText))
            {
                foreach (var messages in Messages.Reverse())
                {
                    if (User.Id == messages.UserId)
                    {
                        if (User.Id == messages.UserId)
                        {
                            EditMessage(messages);
                            return;
                        }
                    }
                }
            }
        }

        public DateTimeOffset ExpireTime { get; set; }

        private Func<Exception, Task> connectionOnClosed()
        {
            return async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                try
                {
                    await _connection.StartAsync();
                }
                catch (Exception e)
                {
                    IsShowingLoginPage = true;
                    User.ErrorMessageLoginPage.IsError = true;
                    User.ErrorMessageLoginPage.GetErrorMessage(e.ToStatusCode().ToString());
                    RegisterUser.ErrorMessageRegisterPage.IsError = true;
                    RegisterUser.ErrorMessageRegisterPage.GetErrorMessage(e.ToStatusCode().ToString());
                }
            };
        }

        private HubConnection _connection;

        private readonly ISkillChatApiClient serviceClient;
        private IChatHub _hub;

        public bool IsConnected { get; set; }

        public bool IsSignedIn { get; set; }

        public enum SidebarSection
        {
            Chats,
            Profile,
            Settings
        }

        [AlsoNotifyFor(nameof(SidebarWidth))]
        public bool IsSidebarExpanded { get; set; } = true;

        public double SidebarWidth => IsSidebarExpanded ? 200 : 48;

        public SidebarSection ActiveSidebarSection { get; set; } = SidebarSection.Chats;

        public bool IsChatsSelected { get; set; } = true;
        public bool IsProfileSelected { get; set; }
        public bool IsSettingsSelected { get; set; }

        public void ToggleSidebar()
        {
            IsSidebarExpanded = !IsSidebarExpanded;
        }

        public ICommand ToggleSidebarCommand { get; }

        public void ShowChats()
        {
            ProfileViewModel.Close();
            SettingsViewModel.Close();
            Width(false);
            SyncSidebarSelection();
        }

        public ICommand ShowChatsCommand { get; }

        public void SyncSidebarSelection()
        {
            if (SettingsViewModel?.IsOpened == true)
            {
                ActiveSidebarSection = SidebarSection.Settings;
            }
            else if (ProfileViewModel?.IsOpened == true)
            {
                ActiveSidebarSection = SidebarSection.Profile;
            }
            else
            {
                ActiveSidebarSection = SidebarSection.Chats;
            }

            IsChatsSelected = ActiveSidebarSection == SidebarSection.Chats;
            IsProfileSelected = ActiveSidebarSection == SidebarSection.Profile;
            IsSettingsSelected = ActiveSidebarSection == SidebarSection.Settings;
        }


        public bool AttachMenuVisible { get; set; }

        public void AttachFileClick()
        {
            AttachMenuVisible = false;
        }

        public void AttachMenuCommand()
        {
            AttachMenuVisible = !AttachMenuVisible;
        }
        public ObservableCollection<MessageViewModel> Messages { get; set; }

        public MessageViewModel SelectedQuotedMessage { get; set; }

        public TokenResult Tokens { get; set; }

        public string Title { get; set; }

        public string MessageText { get; set; }

        public string ChatId { get; set; }
        public string ChatName { get; set; }
        public string MembersCaption { get; set; }

        public ICommand ConnectCommand { get; }

        public ICommand EditLastMessageCommand { get; }

        public ICommand SendCommand { get; }

        public ICommand EndEditCommand { get; }

        public bool KeySendMessage { get; set; }
        public bool AutoScrollWhenSendingMyMessage { get; set; }
        public string InitialUnreadBoundaryMessageId { get; set; }

        public bool IsEdited => idEditMessage != null;

        public ICommand LoadMessageHistoryCommand { get; }

        public ICommand SignOutCommand { get; }

        public ICommand MessageCleaningCommand { get; }

        public ICommand SelectedMessagesDeleteCommand { get; } 

        public bool windowIsFocused { get; set; }
        public bool HasPendingInitialUnreadBoundaryPositioning => !InitialUnreadBoundaryMessageId.IsNullOrEmpty();

        public static ReactiveCommand<object, Unit> NotifyCommand { get; set; }

        public static ReactiveCommand<object, Unit> PointerPressedCommand { get; set; }

        public ProfileViewModel ProfileViewModel { get; set; }
        public SendAttachmentsViewModel AttachmentViewModel { get; set; }
        public ConfirmationViewModel ConfirmationViewModel { get; set; }

        public bool IsShowingLoginPage { get; set; }
        public bool IsShowingRegisterPage { get; set; }

        public string ValidationError { get; set; }

        public ReactiveCommand<object, Unit> GoToRegisterCommand { get; }
        public ICommand RegisterCommand { get; }

        public SettingsViewModel SettingsViewModel { get; set; }

        /// <summary>
        /// Свойство предоставляет доступ к полям, свойствам и методам экземпляра класса SelectMessages.
        /// </summary>
        public SelectMessages SelectMessagesMode { get; set; }

        /// <summary>
        /// Происходит при добавлении нового сообщения в коллекцию сообщений
        /// </summary>
        public event Action<ReceivedMessageArgs> MessageReceived;

        /// <summary>
        /// Свойство предоставляет доступ к полям, свойствам и методам экземпляра класса CurrentUserViewModel
        /// </summary>
        public CurrentUserViewModel User { get; set; }

        /// <summary>
        /// Переменная для хранения Id редактируемого сообщения
        /// </summary>
        private string idEditMessage { get; set; }
        private DateTimeOffset? lastRequestedReadMarkerTime;

        public bool IsEditMessage => !idEditMessage.IsNullOrEmpty();

        /// <summary>
        /// Переменная для хранения флага - было ли задано положение курсора.
        /// </summary>
        public bool IsCursorSet { get; set; }

        /// <summary>
        /// Флаг отвечающий за режим цитирования сообщений
        /// </summary>
        public bool IsSelectQuotedMessage => SelectedQuotedMessage != null;

        /// <summary>
        /// Словарь состоящий из всех сообщений
        /// </summary>
        private Dictionary<string, MessageViewModel> messageDictionary = new Dictionary<string, MessageViewModel>();

        /// <summary>
        /// Метод включает режим выбора сообщений из контектного меню, вызываемого кнопкой "..." на панели главного окна приложения
        /// </summary>
        public void SelectModeOn()
        {
            SelectMessagesMode.IsTurnedSelectMode = true;
            SettingsViewModel.CloseContextMenu();
        }

        /// <summary>
        /// Выбирает из коллекции сообщение, выбранное пользователем и выводит его текст в MessageText
        /// </summary>
        public void EditMessage(MessageViewModel message)
        {
            idEditMessage = message?.Id;
            MessageText = message?.Text;
            IsCursorSet = false;
            if (message != null)
            {
                if (message.IsQuotedMessage) QuoteMessage(message.QuotedMessage);
            }
            else
            {
                CancelQuoted();
            }
        }
        /// <summary>
        /// Начало режима цитирования
        /// </summary>
        /// <param name="message"></param>
        public void QuoteMessage(MessageViewModel message)
        {
            SelectedQuotedMessage = message;
        }
        /// <summary>
        /// Выход из режима цитирования
        /// </summary>
        public void CancelQuoted()
        {
            SelectedQuotedMessage = null;
        }

        public RegisterUserViewModel RegisterUser { get; set; }

        /// <summary>
        /// Сброс сообщения об ошибке и стиля "Error" для элементов View
        /// </summary>
        public void ResetErrorCommand()
        {
            User.ErrorMessageLoginPage.ResetDisplayErrorMessage();
            RegisterUser.ErrorMessageRegisterPage.ResetDisplayErrorMessage();
            User.ErrorMessageLoginPage.IsError = false;
            RegisterUser.ErrorMessageRegisterPage.IsError = false;
        }

        public enum WindowState
        {
            SignOut,
            OpenProfile,
            WindowSettings,
            HeaderMenuPopup
        }

        public bool IsFirstRun { get; set; } = true;

        private static ClientContext ResolveClientContext()
        {
            var bits = Environment.Is64BitOperatingSystem ? "PC 64bit, " : "PC 32bit, ";
            var operatingSystem = bits + RuntimeInformation.OSDescription;

            string ipAddress = string.Empty;
            try
            {
                using var webClient = new WebClient();
                ipAddress = webClient.DownloadString("https://api.ipify.org");
            }
            catch
            {
                try
                {
                    IPHostEntry ipHost = Dns.GetHostEntry("localhost");
                    if (ipHost.AddressList.Length > 0)
                    {
                        ipAddress = ipHost.AddressList.Last().ToString();
                    }
                }
                catch
                {
                }
            }

            return new ClientContext(operatingSystem, ipAddress, "SkillChat Avalonia Client 1.0");
        }

        private sealed record ClientContext(string OperatingSystem, string IpAddress, string ClientName);

        public void WindowStates(WindowState state)
        {
            switch (state)
            {
                case WindowState.SignOut:
                    SettingsViewModel.Close();
                    ProfileViewModel.ContextMenuClose();
                    ProfileViewModel.Close();
                    ResetUnreadBoundaryState();
                    IsFirstRun = true;
                    SyncSidebarSelection();
                    break;
                case WindowState.OpenProfile:
                    SettingsViewModel.Close();
                    SettingsViewModel.CloseContextMenu();
                    Width(SettingsViewModel.IsOpened);
                    SyncSidebarSelection();
                    break;
                case WindowState.WindowSettings:
                    ProfileViewModel.Close();
                    Width(SettingsViewModel.IsOpened);
                    SyncSidebarSelection();
                    break;
                case WindowState.HeaderMenuPopup:
                    ProfileViewModel.ContextMenuClose();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        public string ColumndefinitionWidth { get; set; }
        public string ColumndefinitionWidth2 { get; set; }
        public double? GridWidth { get; set; }
        public string TextHeaderMain { get; set; } = "Чат";
        public bool SettingsActive { get; set; }
        public string TextHeaderMenuInSettings { get; set; }

        public void Width(bool isWindow)
        {
            if (!isWindow)
            {
                ColumndefinitionWidth = "*";
                ColumndefinitionWidth2 = "Auto";
                TextHeaderMain = "Чат";
                SettingsActive = false;
                GridWidth = 388;
            }
            else
            {
                ColumndefinitionWidth = "310";
                ColumndefinitionWidth2 = "*";
                TextHeaderMain = "Настройки";
                SettingsActive = true;
                GridWidth = null;
                TextHeaderMenuInSettings = "Сообщения и чаты";
            }
        }

        public void CompleteInitialUnreadBoundaryPositioning()
        {
            InitialUnreadBoundaryMessageId = null;
        }

        public void ResetUnreadBoundaryState()
        {
            ClearUnreadBoundary();
            lastRequestedReadMarkerTime = null;
        }

        public void ClearUnreadBoundary()
        {
            if (Messages == null)
            {
                InitialUnreadBoundaryMessageId = null;
                return;
            }

            foreach (var message in Messages)
            {
                message.IsUnreadBoundary = false;
            }

            InitialUnreadBoundaryMessageId = null;
        }

        public async Task TryMarkChatReadAsync()
        {
            if (_hub == null || ChatId.IsNullOrEmpty() || SettingsViewModel?.AutoScroll != true)
            {
                return;
            }

            var newestMessage = Messages.LastOrDefault();
            DateTimeOffset requestedReadMarkerTime;

            if (newestMessage == null)
            {
                if (HasPendingInitialUnreadBoundaryPositioning || lastRequestedReadMarkerTime.HasValue)
                {
                    return;
                }

                requestedReadMarkerTime = DateTimeOffset.UtcNow;
            }
            else
            {
                if (lastRequestedReadMarkerTime is DateTimeOffset lastRequested &&
                    lastRequested >= newestMessage.PostTime)
                {
                    return;
                }

                requestedReadMarkerTime = newestMessage.PostTime;
            }

            try
            {
                await _hub.MarkChatRead(ChatId, requestedReadMarkerTime);
                lastRequestedReadMarkerTime = requestedReadMarkerTime;

                if (newestMessage != null)
                {
                    ClearUnreadBoundary();
                }
            }
            catch
            {
            }
        }

        public async Task OpenFileBrowserMenu()
        {
            var canOpenFileDialog = Locator.Current.GetService<ICanOpenFileDialog>();
            var attachmentsPatch = await canOpenFileDialog.Open();
            await AttachmentViewModel.Open(attachmentsPatch);

            AttachMenuVisible = false;
        }

        private async Task<MessagePage> LoadMessageHistoryPageAsync(
            IMapper mapper,
            DateTimeOffset? beforePostTime,
            string? unreadBoundaryMessageId = null)
        {
            var request = new GetMessages
            {
                ChatId = ChatId,
                BeforePostTime = beforePostTime
            };

            var result = await serviceClient.GetMessagesAsync(request);
            var boundaryMessageId = unreadBoundaryMessageId ?? result.FirstUnreadMessageId;

            foreach (var item in result.Messages)
            {
                var newMessage = ToMessageViewModel(mapper, item);
                if (!boundaryMessageId.IsNullOrEmpty() &&
                    string.Equals(item.Id, boundaryMessageId, StringComparison.Ordinal))
                {
                    newMessage.IsUnreadBoundary = true;
                }

                if (item.QuotedMessage != null)
                {
                    newMessage.QuotedMessage = ToMessageViewModel(mapper, item.QuotedMessage);
                }

                if (Messages.Count != 0 && Messages.First().UserId != newMessage.UserId)
                {
                    Messages.First().ShowNickname = true;
                }

                Messages.Insert(0, newMessage);
            }

            if (Messages.Count != 0)
            {
                Messages.First().ShowNickname = true;
            }

            return result;
        }

        private async Task EnsureUnreadBoundaryMessageIsLoadedAsync(
            IMapper mapper,
            string unreadBoundaryMessageId,
            bool hasMoreBefore)
        {
            while (Messages.All(message => !string.Equals(message.Id, unreadBoundaryMessageId, StringComparison.Ordinal)) &&
                   hasMoreBefore)
            {
                var oldestLoadedMessage = Messages.FirstOrDefault();
                if (oldestLoadedMessage == null)
                {
                    return;
                }

                var previousOldestMessageId = oldestLoadedMessage.Id;
                var result = await LoadMessageHistoryPageAsync(
                    mapper,
                    oldestLoadedMessage.PostTime,
                    unreadBoundaryMessageId);
                hasMoreBefore = result.HasMoreBefore;

                var newOldestMessage = Messages.FirstOrDefault();
                if (newOldestMessage == null ||
                    string.Equals(newOldestMessage.Id, previousOldestMessageId, StringComparison.Ordinal))
                {
                    return;
                }
            }
        }

        private MessageViewModel ToMessageViewModel(IMapper mapper, MessageMold messageMold)
        {
            MessageViewModel messageViewModel = mapper.Map<MessageViewModel>(messageMold);
            messageViewModel.IsMyMessage = User.Id == messageMold.UserId;
            if (messageDictionary.TryGetValue(messageMold.Id, out var message))
            {
                mapper.Map(messageViewModel, message);
                messageViewModel = message;
            }
            else
            {
                messageDictionary[messageViewModel.Id] = messageViewModel;
            }

            messageViewModel.Attachments = messageMold.Attachments?
                .Select(s =>
                {
                    var newAttachment = new AttachmentMessageViewModel(s);
                    newAttachment.IsMyMessage = messageViewModel.IsMyMessage;
                    return newAttachment;
                }).ToList();

            return messageViewModel;
        }
    }

    /// <summary>
    /// Хранилище аргументов события MessageReceived
    /// </summary>
    public class ReceivedMessageArgs
    {
        public ReceivedMessageArgs(MessageViewModel message)
        {
            Message = message;
        }

        public MessageViewModel Message { get; set; }
    }
}
