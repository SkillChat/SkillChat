using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using PropertyChanged;
using ReactiveUI;
using ServiceStack;
using SignalR.EasyUse.Client;
using SkillChat.Interface;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;
using Splat;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class MainWindowViewModel
    {
        IConfiguration configuration;
        ChatClientSettings settings;

        public MainWindowViewModel()
        {
            User = new CurrentUserViewModel();
            configuration = Locator.Current.GetService<IConfiguration>();
            settings = configuration.GetSection("ChatClientSettings").Get<ChatClientSettings>();

            if (settings == null)
            {
                settings = new ChatClientSettings();
            }

            if (settings.HostUrl.IsNullOrEmpty())
            {
                settings.HostUrl = "http://localhost:5000";
            }

            serviceClient = new JsonServiceClient(settings.HostUrl);
            ProfileViewModel = new ProfileViewModel(serviceClient, this);

            SettingsViewModel = new SettingsViewModel(serviceClient);
            SettingsViewModel.ProfileViewModel = ProfileViewModel;
            SettingsViewModel.IsWindowSettingsEvent += (e) => { ProfileViewModel.isOpenProfile = false; };
            SettingsViewModel.TypeEnterEvent += (e) => { KeySendMessage = e; };
     
            User.UserName = settings.UserName;
            Tokens = new TokenResult {AccessToken = settings.AccessToken, RefreshToken = settings.RefreshToken};

            Messages = new ObservableCollection<IMessagesContainerViewModel>();
            
            //Магия реактивных штук! Тут происходит подписка на событие добавленя статуса сообщения,
            //если статусы добавляются быстро (быстрее чем 0,2 сек), то они копятся в словаре.
            //Когда после добавления статуса проходит 0,2 сек, из словаря берется коллекция статусов и отправляется на сервер.
            var obs = Observable.FromEvent(
                    handler => AddNewStatus += handler,
                    handler => AddNewStatus -= handler)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .Subscribe(async _ => await SendStatuses());


            ConnectCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                statuses = new ConcurrentDictionary<string, MessageStatus>();
                try
                {
                    _connection = new HubConnectionBuilder()
                        .WithUrl(settings.HostUrl + "/ChatHub")
                        .Build();

                    _hub = _connection.CreateHub<IChatHub>();

                    if (Tokens == null || Tokens.AccessToken.IsNullOrEmpty())
                    {
                        Tokens = await serviceClient.PostAsync(new AuthViaPassword
                            {Login = User.UserName, Password = User.Password});
                        settings.AccessToken = Tokens.AccessToken;
                        settings.RefreshToken = Tokens.RefreshToken;
                        settings.UserName = User.UserName;
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
                        if (data.Error)
                        {
                            IsSignedIn = false;
                            serviceClient.BearerToken = Tokens.RefreshToken;
                            try
                            {
                                Tokens = await serviceClient.PostAsync(new PostRefreshToken());
                                settings.AccessToken = Tokens.AccessToken;
                                settings.RefreshToken = Tokens.RefreshToken;
                                configuration.GetSection("ChatClientSettings").Set(configuration);
                                await _hub.Login(Tokens.AccessToken);
                            }
                            catch (Exception e)
                            {
                                Tokens = null;
                            }
                        }
                        else
                        {
                            IsSignedIn = true;
                            User.UserName = data.UserLogin;
                            ExpireTime = data.ExpireTime;
                            var chats = await serviceClient.GetAsync(new GetChatsList());
                            var chat = chats.Chats.FirstOrDefault();
                            ChatId = chat?.Id;
                            ChatName = chat?.ChatName;
                            LoadMessageHistoryCommand.Execute(null);
                            //Получаем профиль
                            ProfileViewModel.Profile = await serviceClient.GetAsync(new GetMyProfile());
                            //Получаем настройки
                            SettingsViewModel.ChatSettings = await serviceClient.GetAsync(new GetMySettings());
                        }
                    });

                    _connection.Subscribe<MessageStatus>(ApplyMessageStatus);
                    
                    _connection.Subscribe<ReceiveMessage>(async data =>
                    {
                        var isMyMessage = User.UserName.ToLowerInvariant() == data.UserLogin;
                        var newMessage = isMyMessage
                            ? (MessageViewModel) new MyMessageViewModel()
                            : new UserMessageViewModel();
                        newMessage.Id = data.Id;
                        newMessage.Text = data.Message;
                        newMessage.PostTime = data.PostTime;
                        newMessage.UserLogin = data.UserLogin;
                        var container = Messages.LastOrDefault();
                        if (isMyMessage)
                        {
                            if (!(container is MyMessagesContainerViewModel))
                            {
                                container = new MyMessagesContainerViewModel();
                                Messages.Add(container);
                            }
                        }
                        else
                        {
                            if (container is UserMessagesContainerViewModel)
                            {
                                var lastMessage = container.Messages.LastOrDefault();
                                if (lastMessage?.UserLogin != newMessage.UserLogin)
                                {
                                    container = new UserMessagesContainerViewModel();
                                    Messages.Add(container);
                                }
                            }
                            else
                            {
                                container = new UserMessagesContainerViewModel();
                                Messages.Add(container);
                            }

                            if (!windowIsFocused)
                                await Notification.Manager.Show(
                                    $"{(newMessage.UserLogin != null && newMessage.UserLogin.Length > 10 ? string.Concat(newMessage.UserLogin.Remove(10, newMessage.UserLogin.Length - 10), "...") : newMessage.UserLogin)} : ",
                                    $"\"{(newMessage.Text.Length > 10 ? string.Concat(newMessage.Text.Remove(10, newMessage.Text.Length - 10), "...") : newMessage.Text)}\"");
                        }

                        container.Messages.Add(newMessage);
                        if (container.Messages.First() == newMessage)
                        {
                            newMessage.ShowLogin = true;
                        }

                        if (newMessage is UserMessageViewModel userMessage)
                        {
                            userMessage.ReceiveAction += OnReceiveMessage;
                            userMessage.ReadAction += OnReadMessage;
                        }
                        MessageReceived?.Invoke(new ReceivedMessageArgs(newMessage));
                    });

                    _connection.Closed += connectionOnClosed();
                    await _connection.StartAsync();
                    await _hub.Login(Tokens.AccessToken);
                    //Messages.Add("Connection started");
                    IsShowingLoginPage = false;
                    IsShowingRegisterPage = false;
                    ValidationError = "";
                    IsConnected = true;
                    User.Password = "";
                }
                catch (Exception ex)
                {                    
                    //Изменение параеметров TextBox в случае ошибки                    
                    User.Error("неверный логин или пароль");
                    ErrorBe?.Invoke();
                    IsShowingLoginPage = _connection.State != HubConnectionState.Connected ? true : false;
                    //Messages.Add(ex.Message);
                }
            }, this.WhenAnyValue(m => m.IsConnected, b => b == false));

            if (Tokens.AccessToken.IsNullOrEmpty() == false)
            {
                ConnectCommand.Execute(null);
            }

            SendCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    try
                    {
                        await _hub.SendMessage(MessageText, ChatId);
                        MessageText = null;
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
                    var first = Messages.FirstOrDefault()?.Messages.FirstOrDefault();
                    var request = new GetMessages()
                    {
                        ChatId = ChatId
                    };
                    // Логика выбора сообщений по id чата
                    request.BeforePostTime = first?.PostTime;
                    var result = await serviceClient.GetAsync(request);
                    foreach (var item in result.Messages)
                    {
                        var isMyMessage = User.UserName.ToLowerInvariant() == item.UserLogin;
                        var newMessage = isMyMessage
                            ? (MessageViewModel) new MyMessageViewModel()
                            : new UserMessageViewModel();
                        newMessage.Id = item.Id;
                        newMessage.Text = item.Text;
                        newMessage.PostTime = item.PostTime;
                        newMessage.UserLogin = item.UserLogin;
                        var container = Messages.FirstOrDefault();
                        if (isMyMessage)
                        {
                            if (!(container is MyMessagesContainerViewModel))
                            {
                                container = new MyMessagesContainerViewModel();
                                Messages.Insert(0, container);
                            }
                        }
                        else
                        {
                            if (container is UserMessagesContainerViewModel)
                            {
                                var firstMessage = container.Messages.FirstOrDefault();
                                if (firstMessage?.UserLogin != newMessage.UserLogin)
                                {
                                    container = new UserMessagesContainerViewModel();
                                    Messages.Insert(0, container);
                                }
                            }
                            else
                            {
                                container = new UserMessagesContainerViewModel();
                                Messages.Insert(0, container);
                            }
                        }

                        container.Messages.Insert(0, newMessage);

                        var firstInBlock = container.Messages.First();
                        foreach (var message in container.Messages)
                        {
                            message.ShowLogin = firstInBlock == message;
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

            // Скрывает окно регистрации
            IsShowingRegisterPage = false;
            IsShowingLoginPage = true;
            GoToRegisterCommand = ReactiveCommand.Create<object>(_ =>
            {
                IsShowingRegisterPage = true;
                IsShowingLoginPage = false;
                RegisterUser.Login = User.UserName;
                User.Password = "";
            });

            RegisterUser = new RegisterUserViewModel();
            RegisterUser.GoToLoginCommand = ReactiveCommand.Create<object>(_ =>
            {
                IsShowingRegisterPage = false;
                IsShowingLoginPage = true;
                RegisterUser.Password = "";
                User.UserName = RegisterUser.Login;
            });
            IsConnected = false; //Скрывает окно чата            
            RegisterCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var request = new RegisterNewUser();
                try
                {
                    ValidationError = "";
                    if (string.IsNullOrWhiteSpace(RegisterUser.Login) ||
                        string.IsNullOrWhiteSpace(RegisterUser.Password))
                        throw new Exception("Не заполнены логин и/или пароль");
                    request.Login = RegisterUser.Login;
                    request.Password = RegisterUser.Password;
                    request.UserName = RegisterUser.UserName;

                    Tokens = await serviceClient.PostAsync(request);

                    settings.AccessToken = Tokens.AccessToken;
                    settings.RefreshToken = Tokens.RefreshToken;
                    settings.UserName = RegisterUser.Login;
                    configuration.GetSection("ChatClientSettings").Set(settings);
                    ConnectCommand.Execute(null);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка регистрации {ex.Message}");
                    ValidationError = ex.Message;
                }

            });

            NotifyCommand = ReactiveCommand.Create<object>(obj =>
            {
                if (obj is IHaveIsActive win)
                {
                    Notify.WindowIsActive = win.IsActive;
                }
            });
            MorePointerPressedCommand = ReactiveCommand.Create<object>(obj =>
            {
                SettingsViewModel.IsOpenSettings = false;
            });
        }

        /// <summary>Отправка листа статусов на сервер</summary>
        private async Task SendStatuses()
        {
            //Если в данный момент происходит отправка, то не выполняем метод
            if (statuses.Count != 0 && IsStatusesSended)
            {
                var list = statuses.Values.ToList<MessageStatus>();
                IsStatusesSended = false;
                await _hub.SendStatuses(list);
                statuses.Clear();
                IsStatusesSended = true;
            }
        }
        /// <summary>Когда сообщение получает статус прочитано</summary>
        /// <param name="message">Сообщение</param>
        private void OnReadMessage(UserMessageViewModel message)
        {
            if (message.Read)
            {
                if (statuses.TryGetValue(message.Id, out var value) && value.ReadDate == null)
                {
                    value.ReadDate ??= DateTimeOffset.Now;
                }
                else
                {
                    statuses.TryAdd(message.Id, new MessageStatus()
                    {
                        MessageId = message.Id,
                        ReadDate = DateTimeOffset.Now,
                    });
                }
                AddNewStatus?.Invoke();
            }
        }
        /// <summary>Когда сообщение получает статус получено</summary>
        /// <param name="message">Сообщение</param>
        private void OnReceiveMessage(UserMessageViewModel message)
        {
            if (message.Received)
            {
                if (statuses.TryGetValue(message.Id, out var value) && value.ReceivedDate == null)
                {
                    value.ReceivedDate ??= DateTimeOffset.Now;
                }
                else
                {
                    statuses.TryAdd(message.Id, new MessageStatus()
                    {
                        MessageId = message.Id,
                        ReceivedDate = DateTimeOffset.Now,
                    });
                }
                AddNewStatus?.Invoke();
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
                }
            };
        }

        private HubConnection _connection;

        private readonly IJsonServiceClient serviceClient;
        private IChatHub _hub;

        public bool IsConnected { get; set; }

        public bool IsSignedIn { get; set; }

        public ObservableCollection<IMessagesContainerViewModel> Messages { get; set; }

        public TokenResult Tokens { get; set; }

        public string Title => IsSignedIn ? $"SkillChat - {User.UserName}[{ChatName}]" : $"SkillChat";

        public string MessageText { get; set; }

        public string ChatId { get; set; }
        public string ChatName { get; set; }
        public string MembersCaption { get; set; }

        public ICommand ConnectCommand { get; }

        public ICommand SendCommand { get; }

        public bool KeySendMessage { get; set; }

        public ICommand LoadMessageHistoryCommand { get; }
        public ICommand SignOutCommand { get; }
        public bool windowIsFocused { get; set; }
        public static ReactiveCommand<object, Unit> NotifyCommand { get; set; }

        public static ReactiveCommand<object, Unit> MorePointerPressedCommand { get; set; }

        public ProfileViewModel ProfileViewModel { get; set; }

        public bool IsShowingLoginPage { get; set; }
        public bool IsShowingRegisterPage { get; set; }

        public string ValidationError { get; set; }
        public ReactiveCommand<object, Unit> GoToRegisterCommand { get; }
        public RegisterUserViewModel RegisterUser { get; set; }
        public ICommand RegisterCommand { get; }

        public SettingsViewModel SettingsViewModel { get; set; }

        /// <summary>Происходит при добавлении нового сообщения в коллекцию сообщений</summary>
        public event Action<ReceivedMessageArgs> MessageReceived;

        public CurrentUserViewModel User { get; set; }

        /// <summary>
        /// Сброс параметров
        /// </summary>
        public void ResetErrorCommand()
        {
            User.Reset();
            ResetError?.Invoke();
        }
        
        public event Action ErrorBe;
        public event Action ResetError;
        
        /// <summary>Применение сообщению статуса</summary>
        /// <param name="status">Статус полученный от сервера</param>
        void ApplyMessageStatus(MessageStatus status)
        {
            foreach (var container in Messages)
            {
                var message = container.Messages.FirstOrDefault(m => m.Id == status.MessageId);
                if (message != null && message is MyMessageViewModel mymess)
                {
                    mymess.SetStatus(status);
                }
            }
        }

        /// <summary>Список статусов для отправки на сервер</summary>
        private ConcurrentDictionary<string, MessageStatus> statuses;

        private bool IsStatusesSended { get; set; } = true;
        /// <summary>Происходит при добавлении нового статуса для сообщения</summary>
        public event Action AddNewStatus;
    }

    /// <summary>Хранилище аргументов соытия MessageReceived</summary>
    public class ReceivedMessageArgs
    {
        public ReceivedMessageArgs(MessageViewModel message)
        {
            Message = message;
        }

        public MessageViewModel Message { get; set; }
    }
}
