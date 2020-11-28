﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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
            UserName = settings.UserName;
            Tokens = new TokenResult { AccessToken = settings.AccessToken, RefreshToken = settings.RefreshToken };

            Messages = new ObservableCollection<IMessagesContainerViewModel>();
            ConnectCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                try
                {
                    _connection = new HubConnectionBuilder()
                        .WithUrl(settings.HostUrl + "/ChatHub")
                        .Build();

                    _hub = _connection.CreateHub<IChatHub>();

                    if (Tokens == null || Tokens.AccessToken.IsNullOrEmpty())
                    {
                        Tokens = await serviceClient.GetAsync(new GetToken { Login = UserName });
                        settings.AccessToken = Tokens.AccessToken;
                        settings.RefreshToken = Tokens.RefreshToken;
                        settings.UserName = UserName;
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
                            UserName = data.UserLogin;
                            ExpireTime = data.ExpireTime;
                            LoadMessageHistoryCommand.Execute(null);
                        }
                    });

                    _connection.Subscribe<ReceiveMessage>(data =>
                    {
                        var isMyMessage = UserName.ToLowerInvariant() == data.UserLogin;
                        var newMessage = isMyMessage
                            ? (MessageViewModel)new MyMessageViewModel()
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
                        }
                        container.Messages.Add(newMessage);
                        if (container.Messages.First() == newMessage)
                        {
                            newMessage.ShowLogin = true;
                        }
                    });

                    _connection.Closed += connectionOnClosed();
                    await _connection.StartAsync();
                    await _hub.Login(Tokens.AccessToken);
                    //Messages.Add("Connection started");
                    IsConnected = true;
                }
                catch (Exception ex)
                {
                    IsConnected = _connection.State == HubConnectionState.Connected;
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
                    await _hub.SendMessage(MessageText);
                    MessageText = null;
                }
                catch (Exception ex)
                {
                    IsConnected = false;
                    //Messages.Add(ex.Message);
                }
            }, this.WhenAnyValue(m => m.IsConnected, m => m.MessageText, (b, m) => b == true && !string.IsNullOrEmpty(m)));

            LoadMessageHistoryCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                try
                {
                    var first = Messages.FirstOrDefault()?.Messages.FirstOrDefault();
                    var request = new GetMessages();
                    request.BeforePostTime = first?.PostTime;
                    var result = await serviceClient.GetAsync(request);
                    foreach (var item in result.Messages)
                    {
                        var isMyMessage = UserName.ToLowerInvariant() == item.UserLogin;
                        var newMessage = isMyMessage
                            ? (MessageViewModel)new MyMessageViewModel()
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
                    IsConnected = false;
                }
            });

            #region Task4  Для Регистрации
            ///В команде отрывающей окно регистрации необходиммо:
            /// IsRegistration Изменить на true
            /// RegisterUser = new RegisterUserViewModel();

            
            IsSignedIn = false; //Скрывает окно авторизации (Оно должно быти изменено с возможностью авторизации )
            RegisterUser = new RegisterUserViewModel();
            IsRegistration = true;//Скрывает окно регистрации (Оно должно быти изменено с возможностью авторизации )
            RegisterCommand = ReactiveCommand.CreateFromTask(async () =>
            {               
                
                try
                {
                    var request = new RegisterNewUser();
                    request.Login = RegisterUser.RegUserName;
                    request.Password = RegisterUser.Password;
                   /* 
                    Tokens = await serviceClient.PostAsync(request);

                    settings.AccessToken = Tokens.AccessToken;
                    settings.RefreshToken = Tokens.RefreshToken;
                    settings.UserName = RegisterUser.RegUserName;
                    configuration.GetSection("ChatClientSettings").Set(settings);
                    */
                    // Здесь написать редирект на страницу чата с токеном или как там это устроено. Нужно наверно вызвать тоже что и при старом входе
                    UserName = RegisterUser.RegUserName;
                    IsRegistration = true;//скроет окно регистрации
                    IsSignedIn = false;
                }
                catch(Exception ex)
                {
                    /*Для того чтобы не придумывать велосипед для вывода сообщений можно использовать https://www.nuget.org/packages/MessageBox.Avalonia/#*/
                    Debug.WriteLine($"ОШибка регистрации {ex.Message}");                   
                    // ТОлько беда в том что Сервис стэк ошибку не отдает ту что прописана а выдает свою дичь
                    // Это по идее нужно отражать на фронте как ошибки валидации 
                    // например такой пользователь существует или какието другие ошибки? 
                }
            });
            #endregion

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
                    IsConnected = false;
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

        public string UserName { get; set; }
        public string Title => IsSignedIn ? $"SkillChat - {UserName}" : $"SkillChat";

        public string MessageText { get; set; }
        public string MembersCaption { get; set; }

        public ICommand ConnectCommand { get; }

        public ICommand SendCommand { get; }

        public ICommand LoadMessageHistoryCommand { get; }
        public ICommand SignOutCommand { get; }

        #region Для регистрации
        public RegisterUserViewModel RegisterUser { get; set; }
        public ICommand RegisterCommand { get; }

        public bool IsRegistration { get; set; }

        #endregion
    }
}
