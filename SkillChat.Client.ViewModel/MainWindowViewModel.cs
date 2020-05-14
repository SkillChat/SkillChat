using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.AspNetCore.SignalR.Client;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ServiceStack;
using SignalR.EasyUse.Client;
using SkillChat.Interface;
using SkillChat.Server.ServiceModel;

namespace SkillChat.Client.ViewModel
{
    public class MainWindowViewModel : ReactiveObject
    {
        public MainWindowViewModel()
        {
            var hostUrl = "http://localhost:5000";
            _connection = new HubConnectionBuilder()
                .WithUrl(hostUrl + "/ChatHub")
                .Build();

            serviceClient = new JsonServiceClient(hostUrl);

            _connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await _connection.StartAsync();
            };

            var hub = _connection.CreateHub<IChatHub>();

            Messages = new ObservableCollection<MessageViewModel>();
            ConnectCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var tokens = await serviceClient.GetAsync(new GetToken { Login = UserName });
                serviceClient.BearerToken = tokens.AccessToken;
                _connection.Subscribe<ReceiveMessage>(data =>
                {
                    var newMessage = new MessageViewModel
                    {
                        Id = data.Id,
                        Text = data.Message,
                        PostTime = data.PostTime,
                        UserLogin = data.UserLogin,
                    };
                    Messages.Add(newMessage);
                });

                try
                {
                    await _connection.StartAsync();
                    await hub.Login(tokens.AccessToken);
                    //Messages.Add("Connection started");
                    IsConnected = true;
                }
                catch (Exception ex)
                {
                    IsConnected = false;
                    //Messages.Add(ex.Message);
                }

                LoadMessageHistoryCommand.Execute(null);
            }, this.WhenAnyValue(m => m.IsConnected, b => b == false));

            SendCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                try
                {
                    await hub.SendMessage(MessageText);
                }
                catch (Exception ex)
                {
                    IsConnected = false;
                    //Messages.Add(ex.Message);
                }
            }, this.WhenAnyValue(m => m.IsConnected, b => b == true));

            LoadMessageHistoryCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                try
                {
                    var first = Messages.FirstOrDefault();
                    var request = new GetMessages();
                    request.BeforePostTime = first?.PostTime;
                    var result = await serviceClient.GetAsync(request);
                    foreach (var item in result.Messages)
                    {
                        var vm = new MessageViewModel
                        {
                            Id = item.Id,
                            Text = item.Text,
                            PostTime = item.PostTime,
                            UserLogin = item.UserLogin,
                        };
                        Messages.Insert(0, vm);
                    }
                }
                catch (Exception e)
                {
                }
            });
            IsConnected = false;
        }

        readonly HubConnection _connection;

        private readonly IJsonServiceClient serviceClient;

        [Reactive]
        public bool IsConnected { get; set; }

        public ObservableCollection<MessageViewModel> Messages { get; set; }

        [Reactive]
        public string UserName { get; set; }

        [Reactive]
        public string MessageText { get; set; }

        public ICommand ConnectCommand { get; }

        public ICommand SendCommand { get; }

        public ICommand LoadMessageHistoryCommand { get; }
    }
}
