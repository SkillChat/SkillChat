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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;

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

            ProfileViewModel = new ProfileViewModel(serviceClient);
            Locator.CurrentMutable.RegisterConstant<IProfile>(ProfileViewModel);
            ProfileViewModel.IsOpenProfileEvent += () => WindowStates(WindowState.OpenProfile);

            AttachmentViewModel = new SendAttachmentsViewModel(serviceClient);

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
                        var hasAttachments = data.Attachments != null && data.Attachments.Count > 0;

                        MessageViewModel newMessage;

                        if (isMyMessage)
                        {
                            newMessage = hasAttachments ? new MyAttachmentViewModel() : new MyMessageViewModel();
                        }
                        else
                        {
                            newMessage = hasAttachments ? new UserAttachmentViewModel() : new UserMessageViewModel();
                        }

                        newMessage.Id = data.Id;
                        newMessage.Text = data.Message;
                        newMessage.PostTime = data.PostTime;
                        newMessage.UserNickname = data.UserNickname??data.UserLogin;
                        newMessage.UserId = data.UserId;
                        
                        newMessage.Attachments = data.Attachments?
                            .Select(s =>
                            { 
                                var attah = mapper?.Map<AttachmentMold>(s);
                                return new AttachmentMessageViewModel(attah);
                            }).ToList();

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
                        await _hub.SendMessage(new HubMessage(ChatId, MessageText));
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
                    var request = new GetMessages {ChatId = ChatId, BeforePostTime = first?.PostTime};
                    
                    // Логика выбора сообщений по id чата
                    var result = await serviceClient.GetAsync(request);
                    foreach (var item in result.Messages)
                    {
                        var isMyMessage = User.Id == item.UserId;
                        var hasAttachments = item.Attachments != null && item.Attachments.Count > 0;

                        MessageViewModel newMessage;

                        if (isMyMessage)
                        {
                            newMessage = hasAttachments ? new MyAttachmentViewModel() : new MyMessageViewModel();
                        }
                        else
                        {
                            newMessage = hasAttachments ? new UserAttachmentViewModel() : new UserMessageViewModel();
                        }

                        newMessage.Id = item.Id;
                        newMessage.Text = item.Text;
                        newMessage.PostTime = item.PostTime;
                        newMessage.UserNickname = item.UserNickName;
                        newMessage.UserId = item.UserId;

                        newMessage.Attachments = item.Attachments?.Select(s => new AttachmentMessageViewModel(s)).ToList();

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


        public bool AttachMenuVisible { get; set; }

        public void AttachFileClick()
        {
            AttachMenuVisible = false;
        }
        
        public void AttachMenuCommand()
        {
            AttachMenuVisible = !AttachMenuVisible;
        }
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
        public SendAttachmentsViewModel AttachmentViewModel { get; set; }

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

        public async Task OpenFileBrowserMenu()
        {
            var canOpenFileDialog = Locator.Current.GetService<ICanOpenFileDialog>();
            var attachmentsPatch = await canOpenFileDialog.Open();
            await AttachmentViewModel.Open(attachmentsPatch);

            AttachMenuVisible = false;
        }

        public void OpenAttachment(string fileName)
        {
            var path = Path.Combine(settings?.AttachmentDefaultPath, fileName);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        public bool IsExistAttachment(AttachmentMold data) 
        {        
            var fileInfo = new FileInfo(Path.Combine(settings?.AttachmentDefaultPath, data.FileName));
            return fileInfo.Exists && fileInfo.Length == data.Size;
        }

        public async Task<bool> DownloadAttachment(AttachmentMold data)
        {
            try
            {
                //Тут надо убрать префикс получаемого файла
                var pref = "attachment/";
                var attachment = await serviceClient.GetAsync(new GetAttachment { Id = data.Id.Replace(pref, string.Empty) });     
                if (attachment == null) return false;

                var savePath = Path.Combine(settings?.AttachmentDefaultPath, data.FileName);
                var saveFileInfo = new FileInfo(savePath);
                if (!saveFileInfo.Directory.Exists)
                {
                    saveFileInfo.Directory.Create();                    
                }

                if (!string.IsNullOrEmpty(savePath))
                {
                    await using (var fileStream = File.Create(savePath, (int)attachment.Length))
                    {
                        const int bufferSize = 4194304; 
                        var buffer = new byte[bufferSize];
                        attachment.Seek(0, SeekOrigin.Begin);

                        while (attachment.Position < attachment.Length)
                        {
                            var read = await attachment.ReadAsync(buffer, 0, bufferSize);
                            await fileStream.WriteAsync(buffer, 0, read);
                        }

                        await fileStream.FlushAsync();
                    }               
                }

                return true;
            }
            catch (Exception e)
            {
                //TODO вывести ошибку в будущем
                return false;
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
