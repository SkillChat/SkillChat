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
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
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
                settings.AttachmentDefaultPath = "\\Download";
            }

            serviceClient = new JsonServiceClient(settings.HostUrl);
            Locator.CurrentMutable.RegisterConstant(new AttachmentManager(settings.AttachmentDefaultPath, serviceClient));

            ProfileViewModel = new ProfileViewModel(serviceClient);
            Locator.CurrentMutable.RegisterConstant<IProfile>(ProfileViewModel);
            ProfileViewModel.IsOpenProfileEvent += () => WindowStates(WindowState.OpenProfile);

            AttachmentViewModel = new SendAttachmentsViewModel(serviceClient);
            MessageCleaningViewModel = new MessageCleaningViewModel();

            SettingsViewModel = new SettingsViewModel(serviceClient);
            SettingsViewModel.OpenSettingsActiveEvent += (e) => { WindowStates(WindowState.WindowSettings); };
            SettingsViewModel.TypeEnterEvent += (e) => { KeySendMessage = e; };
            SettingsViewModel.ContextMenuSettingsActiveEvent += (e) => { WindowStates(WindowState.HeaderMenuPopup); };
            SettingsViewModel.SetSelectedOnSettingsItemEvent += e => { TextHeaderMenuInSettings = SettingsViewModel.SettingsMenuActiveMain ? "Сообщения и чаты" : "Аудит входа"; };

            Width(false);
            User.Login = settings.Login;
            Tokens = new TokenResult { AccessToken = settings.AccessToken, RefreshToken = settings.RefreshToken };

            Messages = new ObservableCollection<MessageViewModel>();
            var bits = Environment.Is64BitOperatingSystem ? "PC 64bit, " : "PC 32bit, ";
            var operatingSystem = bits + RuntimeInformation.OSDescription;

            string ipAddress = "";
            try
            {
                ipAddress = new WebClient().DownloadString("https://api.ipify.org");
            }
            catch (Exception e)
            {
                try
                {
                    IPHostEntry ipHost = Dns.GetHostEntry("localhost");
                    if (ipHost.AddressList.Length > 0)
                    {
                        ipAddress = ipHost.AddressList.Last().ToString();
                    }
                }
                catch (Exception exception) { }
            }
            var nameVersionClient = "SkillChat Avalonia Client 1.0";

            ConnectCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                try
                {
                    _connection = new HubConnectionBuilder()
                        .WithUrl(settings.HostUrl + "/ChatHub")
                        .Build();

                    _hub = _connection.CreateHub<IChatHub>();
                    ProfileViewModel.SetChatHub(_hub);
                    AttachmentViewModel.SetChatHub(_hub);
                    SelectMessagesMode.SetChatHub(_hub);

                    if (Tokens == null || Tokens.AccessToken.IsNullOrEmpty())
                    {
                        Tokens = await serviceClient.PostAsync(new AuthViaPassword
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
                                    IsSignedIn = false;
                                    serviceClient.BearerToken = Tokens.RefreshToken;
                                    try
                                    {
                                        Tokens = await serviceClient.PostAsync(new PostRefreshToken());
                                        settings.AccessToken = Tokens.AccessToken;
                                        settings.RefreshToken = Tokens.RefreshToken;
                                        configuration.GetSection("ChatClientSettings").Set(configuration);
                                        await _hub.Login(Tokens.AccessToken, operatingSystem, ipAddress, nameVersionClient);
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
                                    IsSignedIn = true;
                                    User.Id = data.Id;
                                    User.Login = data.UserLogin;
                                    ExpireTime = data.ExpireTime;
                                    var chats = await serviceClient.GetAsync(new GetChatsList());
                                    var chat = chats.Chats.FirstOrDefault();
                                    ChatId = chat?.Id;
                                    ChatName = chat?.ChatName;
                                    LoadMessageHistoryCommand.Execute(null);
                                    //Получаем настройки
                                    SettingsViewModel.ChatSettings = await serviceClient.GetAsync(new GetMySettings());
                                    KeySendMessage = SettingsViewModel.ChatSettings.SendingMessageByEnterKey;
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
                                    if (item.ShowNickname) item.UserNickname = user.DisplayName;
                                }
                            }

                            ProfileViewModel.UpdateUserProfile(user.DisplayName, user.Id);
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
                    });

                    _connection.Closed += connectionOnClosed();
                    await _connection.StartAsync();
                    await _hub.Login(Tokens.AccessToken, operatingSystem, ipAddress, nameVersionClient);
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
                    var first = Messages?.FirstOrDefault();
                    var request = new GetMessages { ChatId = ChatId, BeforePostTime = first?.PostTime };

                    // Логика выбора сообщений по id чата
                    var result = await serviceClient.GetAsync(request);

                    foreach (var item in result.Messages)
                    {
                        MessageViewModel ToMessageViewModel(MessageMold messageMold)
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

                        var newMessage = ToMessageViewModel(item);

                        if (item.QuotedMessage != null)
                        {
                            newMessage.QuotedMessage = ToMessageViewModel(item.QuotedMessage);
                        }

                        if (Messages.Count != 0)
                        {
                            if (Messages.First().UserId != newMessage.UserId)
                            {
                                Messages.First().ShowNickname = true;
                            }
                        }

                        Messages.Insert(0, newMessage);


                    }

                    if (Messages.Count != 0) Messages.First().ShowNickname = true;
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

                    Tokens = await serviceClient.PostAsync(request);

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

            MessageCleaningCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                SettingsViewModel.CloseContextMenu();
                SettingsViewModel.IsOpened = false;
                MessageCleaningViewModel.OpenCommand.Execute(null);
            });

            ProfileViewModel.SignOutCommand = SignOutCommand;
            ProfileViewModel.LoadMessageHistoryCommand = LoadMessageHistoryCommand;
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

        private readonly IJsonServiceClient serviceClient;
        private IChatHub _hub;

        public bool IsConnected { get; set; }

        public bool IsSignedIn { get; set; }


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

        public string Title => IsSignedIn ? $"SkillChat - {User.UserName}[{ChatName}]" : $"SkillChat";

        public string MessageText { get; set; }

        public string ChatId { get; set; }
        public string ChatName { get; set; }
        public string MembersCaption { get; set; }

        public ICommand ConnectCommand { get; }

        public ICommand EditLastMessageCommand { get; }

        public ICommand SendCommand { get; }

        public ICommand EndEditCommand { get; }

        public bool KeySendMessage { get; set; }

        public bool IsEdited => idEditMessage != null;

        public ICommand LoadMessageHistoryCommand { get; }

        public ICommand SignOutCommand { get; }

        public ICommand MessageCleaningCommand { get; }

        public bool windowIsFocused { get; set; }

        public static ReactiveCommand<object, Unit> NotifyCommand { get; set; }

        public static ReactiveCommand<object, Unit> PointerPressedCommand { get; set; }

        public ProfileViewModel ProfileViewModel { get; set; }
        public SendAttachmentsViewModel AttachmentViewModel { get; set; }
        public MessageCleaningViewModel MessageCleaningViewModel { get; set; }

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

        public void WindowStates(WindowState state)
        {
            switch (state)
            {
                case WindowState.SignOut:
                    SettingsViewModel.Close();
                    ProfileViewModel.ContextMenuClose();
                    ProfileViewModel.Close();
                    IsFirstRun = true;
                    break;
                case WindowState.OpenProfile:
                    SettingsViewModel.Close();
                    SettingsViewModel.CloseContextMenu();
                    Width(SettingsViewModel.IsOpened);
                    break;
                case WindowState.WindowSettings:
                    ProfileViewModel.Close();
                    Width(SettingsViewModel.IsOpened);
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

        public async Task OpenFileBrowserMenu()
        {
            var canOpenFileDialog = Locator.Current.GetService<ICanOpenFileDialog>();
            var attachmentsPatch = await canOpenFileDialog.Open();
            await AttachmentViewModel.Open(attachmentsPatch);

            AttachMenuVisible = false;
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
