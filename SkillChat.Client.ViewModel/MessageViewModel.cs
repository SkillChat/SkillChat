using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
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

            SelectMsgMode = Locator.Current.GetService<SelectMessages>();

            this.ObservableForProperty(m => m.IsChecked).Subscribe(IsCheckedChanged);
        }

        private void IsCheckedChanged(IObservedChange<MessageViewModel, bool> change)
        {
            if (IsChecked)
            {
                SelectMsgMode.Select(this);
            }
            else
            {
                SelectMsgMode.UnSelect(this);
            }
        }

        public string Id { get; set; }

        public string UserId { get; set; }

        public string UserDisplayName { get; set; }

        public string DisplayNickname => ShowNickname ? UserDisplayName : null;

        public string QuotedDisplayNickname=> !IsMyMessage ? UserDisplayName : "Вы";

        public bool ShowNickname { get; set; }

        public string Text { get; set; }

        public string TextAligned => $"{Text}{Time}";

        public DateTimeOffset PostTime { get; set; }

        public DateTimeOffset? LastEditTime { get; set; }

        public string Time { get; set; }

        public bool Selected { get; set; }

        public bool IsMyMessage { get; set; }

        public SelectMessages SelectMsgMode { get; set; }

        public bool IsChecked { get; set; }

        public enum ViewTypeMessage
        {
            MyFirstMessage,
            MyNotFirstMessage,
            SomeonesFirstMessage,
            SomeonesNotFirstMessage
        }

        public ViewTypeMessage ViewType
        {
            get
            {
                if (IsMyMessage)
                    return ShowNickname ? ViewTypeMessage.MyFirstMessage : ViewTypeMessage.MyNotFirstMessage;
                return ShowNickname ? ViewTypeMessage.SomeonesFirstMessage : ViewTypeMessage.SomeonesNotFirstMessage;
            }
        }

        public bool IsTextNullOrEmpty => Text.IsNullOrEmpty();

        public bool IsAttachmentMessage => Attachments?.Any() ?? false;

        public bool Edited => LastEditTime != null;

        public List<AttachmentMessageViewModel> Attachments { get; set; }
        public UserProfileMold ProfileMold { get; set; }
        public ReactiveCommand<string, Unit> UserProfileInfoCommand { get; set; }

        /// <summary>
        /// Цитируемое сообщение 
        /// </summary>
        public MessageViewModel QuotedMessage { get; set; }

        /// <summary>
        /// Флаг показывает, что данное сообщение отвечает на другое сообщение  
        /// </summary>
        public bool IsQuotedMessage => QuotedMessage != null;

        /// <summary>
        /// Коллекция меню элементов 
        /// </summary>
        public ObservableCollection<MenuItemObject> MenuItems
        {
            get
            {
                var MenuItems = new ObservableCollection<MenuItemObject>(new List<MenuItemObject>());
                if (IsMyMessage)
                {
                    MenuItems.Add(new MenuItemObject { Command = ReactiveCommand.Create<object>(SelectEditMessage), Content = "Редактировать" });
                    MenuItems.Add(new MenuItemObject { Command = ReactiveCommand.Create<object>(SelectQuotedMessage), Content = "Ответить" });
                    MenuItems.Add(new MenuItemObject { Command = ReactiveCommand.Create<object>(SelectMessage), Content = "Выбрать сообщение" });
                }
                else
                {
                    MenuItems.Add(new MenuItemObject { Command = ReactiveCommand.Create<object>(SelectQuotedMessage), Content = "Ответить" });
                    MenuItems.Add(new MenuItemObject { Command = ReactiveCommand.Create<object>(SelectMessage), Content = "Выбрать сообщение" });
                }

                return MenuItems;
            }
        }

        /// <summary>
        /// Редактирование сообщения
        /// </summary>
        /// <param name="o"></param>
        private void SelectEditMessage(object o)
        {
            Selected = true;
            var mw = Locator.Current.GetService<MainWindowViewModel>();
            mw.EditMessage(this);
        }

        /// <summary>
        /// Ответ на сообщение
        /// </summary>
        /// <param name="o"></param>
        private void SelectQuotedMessage(object o)
        {
            var mw = Locator.Current.GetService<MainWindowViewModel>();
            mw.QuoteMessage(this);
        }

        private void SelectMessage(object o)
        {
            SelectMsgMode.IsTurnedSelectMode = true;
            IsChecked = true;
        }
    }

    [AddINotifyPropertyChangedInterface]
    public class AttachmentMessageViewModel : AttachmentMold
    {
        public AttachmentMessageViewModel(AttachmentMold attachment)
        {
            var attachmentManager = Locator.Current.GetService<AttachmentManager>();
            Init(attachment, attachmentManager);
        }

        public AttachmentMessageViewModel(AttachmentMold attachment, AttachmentManager attachmentManager)
        {
            Init(attachment, attachmentManager);
        }

        public void Init(AttachmentMold attachment, AttachmentManager attachmentManager)
        {
            _isDownload = attachmentManager.IsExistAttachment(attachment);

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
                    _isDownload = await attachmentManager.DownloadAttachment(attachment);
                    Text = !_isDownload ? "Загрузить" : "Открыть";
                }
                else attachmentManager.OpenAttachment(attachment.FileName);
            });
        }

        public string SizeName => Size.SizeCalculating();

        public string Extensions => Path.GetExtension(FileName).Replace(".", "").ToUpper();

        public string Text { get; set; }

        public bool IsMyMessage { get; set; }

        private bool _isDownload { get; set; }

        public ReactiveCommand<AttachmentMold, Unit> DownloadCommand { get; set; }
    }
}
