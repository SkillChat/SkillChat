using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reactive;
using PropertyChanged;
using ReactiveUI;
using ServiceStack;
using SkillChat.Interface.Extensions;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.ServiceModel.Molds.Attachment;
using Splat;

namespace SkillChat.Client.ViewModel
{
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
            if (!IsMyMessage)
            {
                UserProfileInfoCommand = ReactiveCommand.Create<string>(async userId =>
                {
                    var profileViewModel = Locator.Current.GetService<IProfile>();
                    await profileViewModel.Open(userId);
                });
            }


        }

        public string Id { get; set; }

        public string UserId { get; set; }

        public string UserNickname { get; set; }

        public string DisplayNickname => ShowNickname ? UserNickname : null;

        public bool ShowNickname { get; set; }

        public string Text { get; set; }

        public string TextAligned => $"{Text}{Time}";

        public DateTimeOffset PostTime { get; set; }

        public DateTimeOffset? LastEditTime { get; set; }

        public string Time { get; set; }

        public bool Selected { get; set; }

        public bool IsMyMessage { get; set; }

        public enum StatusMessage
        {
            MyFirstMessage,
            MyNotFirstMessage,
            SomeonesFirstMessage,
            SomeonesNotFirstMessage
        }

        public StatusMessage Status
        {
            get
            {
                if (IsMyMessage)
                    return ShowNickname ? StatusMessage.MyFirstMessage : StatusMessage.MyNotFirstMessage;
                return ShowNickname ? StatusMessage.SomeonesFirstMessage : StatusMessage.SomeonesNotFirstMessage;
            }
        }

        public bool IsTextNullOrEmpty => Text.IsNullOrEmpty();

        public bool IsAttachmentMessage { get; set; }

        public bool Edited => LastEditTime != null;

        public List<AttachmentMessageViewModel> Attachments { get; set; }
        public UserProfileMold ProfileMold { get; set; }
        public ReactiveCommand<string, Unit> UserProfileInfoCommand { get; set; }

        public void SelectEditMessage()
        {
            Selected = true;
            var mw = Locator.Current.GetService<MainWindowViewModel>();
            mw.EditMessage(this);
        }
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
        public string Text { get; set; }
        public bool IsMyMessage { get; set; }
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