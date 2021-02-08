﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using PropertyChanged;
using ReactiveUI;
using SkillChat.Interface;
using SkillChat.Interface.Extensions;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.ServiceModel.Molds.Attachment;
using Splat;

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
    public class MyAttachmentViewModel : MessageViewModel
    {
    }

    [AddINotifyPropertyChangedInterface]
    public class UserMessageViewModel : MessageViewModel
    {
        public UserMessageViewModel()
        {
            UserProfileInfoCommand = ReactiveCommand.Create<string>(async userId =>
            {
                var profileViewModel = Locator.Current.GetService<IProfile>();
                await profileViewModel.Open(userId);
            });
            this.WhenAnyValue(r => r.Received).Subscribe(_ =>
            {
                ReceiveAction?.Invoke(this);
            });
            this.WhenAnyValue(r => r.Read).Subscribe(_ =>
            {
                ReadAction?.Invoke(this);
            });
        }
        
        public UserProfileMold ProfileMold { get; set; }
        public  ReactiveCommand<string, Unit> UserProfileInfoCommand { get; set; }

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
    public class UserAttachmentViewModel : MessageViewModel {}

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

        public string UserId { get; set; }

        public string UserNickname { get; set; }

        public string DisplayNickname => ShowNickname ? UserNickname : null;

        public bool ShowNickname { get; set; }

        public string Text { get; set; }

        public string TextAligned => $"{Text}{Time}";
        
        public DateTimeOffset PostTime { get; set; }

        public string Time { get; set; }

        public List<AttachmentMessageViewModel> Attachments { get; set; }
    }

    [AddINotifyPropertyChangedInterface]
    public class AttachmentMessageViewModel : AttachmentMold
    {
        public AttachmentMessageViewModel(AttachmentMold attachment)
        {
            var mainWindowViewModel = Locator.Current.GetService<MainWindowViewModel>();
            _isDownload = mainWindowViewModel.IsExistAttachment(attachment);

            Id = attachment.Id;
            SenderId = attachment.SenderId;
            FileName = attachment.FileName;
            Size = attachment.Size;
            UploadDateTime = attachment.UploadDateTime;
            Hash = attachment.Hash;
            Text = !_isDownload ? "Загрузить" : "Открыть";

            DownloadCommand = ReactiveCommand.Create<AttachmentMold>(async attachment =>
            {
                if (!_isDownload)
                {
                    _isDownload = await mainWindowViewModel.DownloadAttachment(attachment);
                    Text = !_isDownload ? "Загрузить" : "Открыть";
                }
                else mainWindowViewModel.OpenAttachment(attachment.FileName);                 
            });


        }

        public string SizeName => Size.SizeCalculating();
        public string Extensions => Path.GetExtension(FileName).Replace(".", "").ToUpper();
        public string Text { get ; set; }

        private bool _isDownload { get; set; }

        private bool _hashDownloadFile(AttachmentMold attachment) 
        {
            var fileInfo = new FileInfo(attachment.FileName);
            var result = fileInfo.Exists;
            return result;
        }

        public ReactiveCommand<AttachmentMold, Unit> DownloadCommand { get; set; }
    }
}
