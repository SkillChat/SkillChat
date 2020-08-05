using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.AspNetCore.SignalR.Client;
using PropertyChanged;
using ReactiveUI;
using ServiceStack;
using SignalR.EasyUse.Client;
using SkillChat.Interface;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class MainWindowViewModel
    {
        public MainWindowViewModel()
        {
            var hostUrl = "http://localhost:5000";
            

            serviceClient = new JsonServiceClient(hostUrl);
            
            

            Messages = new ObservableCollection<IMessagesContainerViewModel>();
            ConnectCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                try
                {
                    _connection = new HubConnectionBuilder()
                        .WithUrl(hostUrl + "/ChatHub")
                        .Build();

                    _hub = _connection.CreateHub<IChatHub>();

                    if (!IsSignedIn)
                    {
                        Tokens = await serviceClient.GetAsync(new GetToken { Login = UserName });
                    }
                    serviceClient.BearerToken = Tokens.AccessToken;
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

                LoadMessageHistoryCommand.Execute(null);
            }, this.WhenAnyValue(m => m.IsConnected, b => b == false));

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
                    serviceClient.BearerToken = null;
                    _connection.Closed -= connectionOnClosed();
                    await _connection.StopAsync();
                    _connection = null;
                    _hub = null;
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    IsConnected = false;
                }
            });
            IsConnected = false;
        }

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

        public bool IsSignedIn => Tokens != null;

        public ObservableCollection<IMessagesContainerViewModel> Messages { get; set; }

        public TokenResult Tokens { get; set; }

        public string UserName { get; set; }
        public string Title => IsSignedIn ? $"SkillChat - {UserName}" : $"SkillChat";

        public string MessageText { get; set; }
        public string MembersCaption { get; set; } //= "Вы, Кристина Петрова, Стас Верещагин, Иван";

        public ICommand ConnectCommand { get; }

        public ICommand SendCommand { get; }

        public ICommand LoadMessageHistoryCommand { get; }
        public ICommand SignOutCommand { get; }
    }
}
