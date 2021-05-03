﻿using System;
using System.Collections.ObjectModel;
using System.Reactive;
using PropertyChanged;
using ReactiveUI;
using ServiceStack;
using SkillChat.Server.ServiceModel;
using SkillChat.Server.ServiceModel.Molds;
using Splat;

namespace SkillChat.Client.ViewModel
{
    public interface IMessagesContainerViewModel
    {
        ObservableCollection<MessageViewModel> Messages { get;set; }
         void SetSelecMessageActiveCommand(bool status);
   }

    [AddINotifyPropertyChangedInterface]
    public class MyMessagesContainerViewModel:IMessagesContainerViewModel
    {
        public MyMessagesContainerViewModel()
        {
            Locator.CurrentMutable.RegisterConstant(this);
        }

        public ObservableCollection<MessageViewModel> Messages { get;set; } = new ObservableCollection<MessageViewModel>();

        public void SetSelecMessageActiveCommand(bool status)
        {
            foreach (var m in Messages)
            {
                m.SelectMessageActive = status;
            }
        }

    }
    
    [AddINotifyPropertyChangedInterface]
    public class UserMessagesContainerViewModel:IMessagesContainerViewModel
    {
        public UserMessagesContainerViewModel()
        {
            Locator.CurrentMutable.RegisterConstant(this);
        }

        public ObservableCollection<MessageViewModel> Messages { get;set; } = new ObservableCollection<MessageViewModel>();

        public void SetSelecMessageActiveCommand(bool status)
        {
            foreach (var m in Messages)
            {
                m.SelectMessageActive = status;
            }
        }
    }

    [AddINotifyPropertyChangedInterface]
    public class MyMessageViewModel : MessageViewModel { }

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
        }

        public UserProfileMold ProfileMold { get; set; }
        public  ReactiveCommand<string, Unit> UserProfileInfoCommand { get; set; }
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
            Locator.CurrentMutable.RegisterConstant(this);
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

        public bool Selected { get; set; }

        public bool SelectMessageActive { get; set; }

        /// <summary>
        /// Включает возможность выборки сообщений
        /// </summary>
        public void SelectMessageCommand()
        {
            var settingVM = Locator.Current.GetService<SettingsViewModel>();
            settingVM.SelectMessageCommandFromContextMenu();
        }               
    }
}
