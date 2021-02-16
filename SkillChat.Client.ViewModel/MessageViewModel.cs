using System;
using System.Collections.ObjectModel;
using PropertyChanged;
using ReactiveUI;
using SkillChat.Interface;

namespace SkillChat.Client.ViewModel
{
    public interface IMessagesContainerViewModel
    {
        ObservableCollection<MessageViewModel> Messages { get;set; }
    }

    [AddINotifyPropertyChangedInterface]
    public class MyMessagesContainerViewModel:IMessagesContainerViewModel
    {
        public ObservableCollection<MessageViewModel> Messages { get;set; } = new ObservableCollection<MessageViewModel>();
    }
    
    [AddINotifyPropertyChangedInterface]
    public class UserMessagesContainerViewModel:IMessagesContainerViewModel
    {
        public ObservableCollection<MessageViewModel> Messages { get;set; } = new ObservableCollection<MessageViewModel>();
    }

    [AddINotifyPropertyChangedInterface]
    public class MyMessageViewModel : MessageViewModel
    {
        public MyMessageViewModel() : base()
        {
            this.WhenAnyValue(r => r.ReadCount).Subscribe(ri =>
            {
                if (ri > 0) IsRead = true;
            });

            this.WhenAnyValue(r => r.ReceivedCount).Subscribe(re =>
            {
                if (re > 0) IsReceived = true;
            });
        }

        /// <summary>Отправлено ли на сервер</summary>
        public bool IsSended { get; set; } = false;
        /// <summary>Сколько раз прочитали</summary>
        public long ReadCount { get; set; }
        /// <summary>Сколько раз получили</summary>
        public long ReceivedCount { get; set; }

        /// <summary>Получено ли</summary>
        public bool IsReceived { get; set; } = false;

        /// <summary>Прочитано ли</summary>
        public bool IsRead { get; set; } = false;

        public void SetStatus(MessageStatus status)
        {
            if (status.ReceivedDate != null && !IsReceived) IsReceived = true;
            if (status.ReadDate != null && IsRead) IsRead = true;
        }
    }

    [AddINotifyPropertyChangedInterface]
    public class UserMessageViewModel : MessageViewModel
    {
        public UserMessageViewModel()
        {
            this.WhenAnyValue(r => r.Received).Subscribe(_ =>
            {
                ReceiveAction?.Invoke(this);
            });
            this.WhenAnyValue(r => r.Read).Subscribe(_ =>
            {
                ReadAction?.Invoke(this);
            });
        }

        /// <summary>Получено ли</summary>
        public bool Received { get; set; } = false;

        /// <summary>Прочитано ли</summary>
        public bool Read { get; set; } = false;

        /// <summary>Возникает при изменении статуса о получении</summary>
        public Action<UserMessageViewModel> ReceiveAction;
        /// <summary>Возникает при измененении статуса о прочтении</summary>
        public Action<UserMessageViewModel> ReadAction;
    }

    [AddINotifyPropertyChangedInterface]
    public class MessageViewModel
    {
        public MessageViewModel()
        {
            this.WhenAnyValue(x => x.PostTime).Subscribe(t =>
            {
                var local = t.ToLocalTime();
                if (local < DateTimeOffset.Now.Date)
                {
                    Time = local.ToString("g");
                }
                else
                {
                    Time = local.ToString("t");
                }
            });
        }

        public string Id { get; set; }

        public string UserLogin { get; set; }

        public string DisplayLogin => ShowLogin ? UserLogin : null;

        public bool ShowLogin { get; set; }

        public string Text { get; set; }

        public string TextAligned => $"{Text}{Time}";
        
        public DateTimeOffset PostTime { get; set; }

        public string Time { get; set; }
    }
}