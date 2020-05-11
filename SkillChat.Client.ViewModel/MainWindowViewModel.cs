using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SignalR.EasyUse.Client;
using SkillChat.Interface;

namespace SkillChat.Client.ViewModel
{
    public class MainWindowViewModel : ReactiveObject
    {
        public MainWindowViewModel()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/ChatHub")
                .Build();

            _connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await _connection.StartAsync();
            };

            var hub = _connection.CreateHub<IChatHub>();

            Messages = new ObservableCollection<string>();
            ConnectCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                _connection.Subscribe<ReceiveMessage>(data =>
                {
                    var newMessage = $"{data.User}: {data.Message}";
                    Messages.Add(newMessage);
                });
                
                try
                {
                    await _connection.StartAsync();
                    Messages.Add("Connection started");
                    IsConnected = true;
                }
                catch (Exception ex)
                {
                    IsConnected = false;
                    Messages.Add(ex.Message);
                }
            }, this.WhenAnyValue(m => m.IsConnected, b => b == false));

            SendCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                try
                {
                    await hub.SendMessage(UserName, MessageText);
                }
                catch (Exception ex)
                {
                    IsConnected = false;
                    Messages.Add(ex.Message);
                }
            }, this.WhenAnyValue(m => m.IsConnected, b => b == true));
            IsConnected = false;
        }

        readonly HubConnection _connection;

        [Reactive]
        public bool IsConnected { get; set; }

        public ObservableCollection<string> Messages { get; set; }

        [Reactive]
        public string UserName { get; set; }

        [Reactive]
        public string MessageText { get; set; }

        public IReactiveCommand ConnectCommand { get; }

        public IReactiveCommand SendCommand { get; }
    }
}
