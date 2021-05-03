﻿using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using PropertyChanged;
using ReactiveUI;
using ServiceStack;
using SignalR.EasyUse.Client;
using SkillChat.Interface;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;
using Splat;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Input.Platform;

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
            Locator.CurrentMutable.RegisterConstant<ICurrentUser>(User);
            Locator.CurrentMutable.RegisterConstant(this);
            configuration = Locator.Current.GetService<IConfiguration>();
            settings = configuration?.GetSection("ChatClientSettings")?.Get<ChatClientSettings>();
            if (settings == null)
            {
                settings = new ChatClientSettings();
            }

            if (settings.HostUrl.IsNullOrEmpty())
            {
                settings.HostUrl = "http://localhost:5000";
            }

            serviceClient = new JsonServiceClient(settings.HostUrl);

            ProfileViewModel = new ProfileViewModel(serviceClient);
            Locator.CurrentMutable.RegisterConstant<IProfile>(ProfileViewModel);
            ProfileViewModel.IsOpenProfileEvent += () => WindowStates(WindowState.OpenProfile);

            SettingsViewModel = new SettingsViewModel(serviceClient);
            SettingsViewModel.OpenSettingsActiveEvent += (e) => { WindowStates(WindowState.WindowSettings); };
            SettingsViewModel.TypeEnterEvent += (e) => { KeySendMessage = e; };
            SettingsViewModel.ContextMenuSettingsActiveEvent += (e) => { WindowStates(WindowState.HeaderMenuPopup); };
            SettingsViewModel.SetSelectedOnSettingsItemEvent += e => { TextHeaderMenuInSettings = SettingsViewModel.SettingsMenuActiveMain ? "Сообщения и чаты" : "Аудит входа"; };

            Width(false);
            User.UserName = settings.UserName;
            Tokens = new TokenResult { AccessToken = settings.AccessToken, RefreshToken = settings.RefreshToken };

            Messages = new ObservableCollection<IMessagesContainerViewModel>();

            var bits = Environment.Is64BitOperatingSystem ? "PC 64bit, " : "PC 32bit, ";
            var operatingSystem = bits + RuntimeInformation.OSDescription;
            var ipAddress = new WebClient().DownloadString("https://api.ipify.org");
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

                    if (Tokens == null || Tokens.AccessToken.IsNullOrEmpty())
                    {
                        Tokens = await serviceClient.PostAsync(new AuthViaPassword
                        { Login = User.UserName, Password = User.Password });
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
                                await _hub.Login(Tokens.AccessToken, operatingSystem, ipAddress, nameVersionClient);
                            }
                            catch (Exception e)
                            {
                                Tokens = null;
                            }
                        }
                        else
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
                        }
                    });

                    _connection.Subscribe<UpdateUserDisplayName>(async user =>
                    {
                        try
                        {
                            var updateMessages = Messages.Where(s => s is UserMessagesContainerViewModel);
                            foreach (var message in updateMessages)
                            {
                                foreach (var item in message.Messages.Where(s => s.UserId == user.Id))
                                {
                                    item.UserNickname = user.DisplayName;
                                }
                            }

                            ProfileViewModel.UpdateUserProfile(user.DisplayName, user.Id);
                        }
                        catch (Exception e)
                        {
                            SignOutCommand.Execute(null);
                        }
                    });

                    _connection.Subscribe<ReceiveMessage>(async data =>
                    {
                        var isMyMessage = User.Id == data.UserId;
                        var newMessage = isMyMessage
                            ? (MessageViewModel)new MyMessageViewModel()
                            : new UserMessageViewModel();
                        newMessage.Id = data.Id;
                        newMessage.Text = data.Message;
                        newMessage.PostTime = data.PostTime;
                        newMessage.UserNickname = data.UserNickname??data.UserLogin;
                        newMessage.UserId = data.UserId;
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
                                if (lastMessage?.UserId != newMessage.UserId)
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

                            if (!windowIsFocused || SettingsViewModel.IsOpened)
                                Notify.NewMessage(newMessage.UserNickname, newMessage.Text.Replace("\r\n", " "));
                        }

                        container.Messages.Add(newMessage);
                        if (container.Messages.First() == newMessage)
                        {
                            newMessage.ShowNickname = true;
                        }

                        MessageReceived?.Invoke(new ReceivedMessageArgs(newMessage));
                    });

                    _connection.Closed += connectionOnClosed();
                    await _connection.StartAsync();
                    await _hub.Login(Tokens.AccessToken, operatingSystem, ipAddress, nameVersionClient);
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
                    var settingVM = Locator.Current.GetService<SettingsViewModel>();
                    settingVM.SetSelectMessahgeActivator();

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
                        var isMyMessage = User.Id == item.UserId;
                        var newMessage = isMyMessage
                            ? (MessageViewModel)new MyMessageViewModel()
                            : new UserMessageViewModel();
                        newMessage.Id = item.Id;
                        newMessage.Text = item.Text;
                        newMessage.PostTime = item.PostTime;
                        newMessage.UserNickname = item.UserNickName;
                        newMessage.UserId = item.UserId;
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
                                if (firstMessage?.UserId != newMessage.UserId)
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
                            message.ShowNickname = firstInBlock == message;
                        }                        
                    }
                    //// Для загруженных сообщений, устанавливаем возможность выборки от её значения
                    var settingVM = Locator.Current.GetService<SettingsViewModel>();
                    settingVM.SetSelectMessahgeActivator();
                    ////
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
                    windowIsFocused = win.IsActive;
                }
            });
            PointerPressedCommand = ReactiveCommand.Create<object>(obj =>
            {
                ProfileViewModel.ContextMenuClose();
                SettingsViewModel.CloseContextMenu();
            });

            ProfileViewModel.SignOutCommand = SignOutCommand;
            ProfileViewModel.LoadMessageHistoryCommand = LoadMessageHistoryCommand;
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

        public static ReactiveCommand<object, Unit> PointerPressedCommand { get; set; }

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

        public enum WindowState
        {
            SignOut,
            OpenProfile,
            WindowSettings,
            HeaderMenuPopup
        }

        public void WindowStates(WindowState state)
        {
            switch (state)
            {
                case WindowState.SignOut:
                    SettingsViewModel.Close();
                    ProfileViewModel.ContextMenuClose();
                    ProfileViewModel.Close();
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

        /// <summary>
        /// Коллекция выбранных сообщений
        /// </summary>
        public ObservableCollection<MessageViewModel> SelectedMessages { get; set; }

        /// <summary>
        /// Текст выбранных сообщений
        /// </summary>
        public string TextSelectedMessages { get; set; }


        
        /// <summary>
        /// Команда добавления в список выбраных сообщений по команде
        /// </summary>
        public void AddToSelectedListCommandByButton()
        {
            TextSelectedMessages ="";
            if (SelectedMessages == null) SelectedMessages = new ObservableCollection<MessageViewModel>();
            var userMessageVM = Locator.Current.GetService<UserMessagesContainerViewModel>();
            var myMessageVM = Locator.Current.GetService<MyMessagesContainerViewModel>();
            if(userMessageVM!=null) AddToSelectedMessagesColl(userMessageVM);
            if (myMessageVM != null) AddToSelectedMessagesColl(myMessageVM);
            var settingVM = Locator.Current.GetService<SettingsViewModel>();
            settingVM.SelectMessageCommandFromContextMenu();
            CopyToClipboard();
            
        }

        /// <summary>
        /// Добавляет текст выбранных сообщение в буфер обмена
        /// </summary>
        public void CopyToClipboard()
        {
          AvaloniaLocator.Current.GetService<IClipboard>().SetTextAsync(TextSelectedMessages);
        }

        /// <summary>
        /// Заполняет колекцию из выбранных сообщений
        /// </summary>
        /// <param name="MessageVM">Колекция сообщений из которых идет выборка</param>
        private void AddToSelectedMessagesColl(IMessagesContainerViewModel MessageVM)
        {
            
            foreach (var message in MessageVM.Messages)
            {
                if (message.Selected)
                {
                    SelectedMessages.Add(message);
                    string text = $"{message.UserNickname}\n {message.Text}\n {message.Time}\n";
                    TextSelectedMessages += text;
                }
                message.Selected = false;
            }
        }
    }

    /// <summary>Хранилище аргументов события MessageReceived</summary>
    public class ReceivedMessageArgs
    {
        public ReceivedMessageArgs(MessageViewModel message)
        {
            Message = message;
        }

        public MessageViewModel Message { get; set; }
    }
}
