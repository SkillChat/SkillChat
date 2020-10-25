using System;
using System.Collections.ObjectModel;
using PropertyChanged;
using SkillChat.Server.ServiceModel.Molds;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class DesignMainWindowViewModel : MainWindowViewModel
    {
        public DesignMainWindowViewModel()
        {
            IsConnected = true;
            Tokens = new TokenResult();
            MembersCaption = "Вы, Кристина Петрова, Стас Верещагин, Иван";
            MessageText =
                    "Привет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\n";

            Messages = new ObservableCollection<IMessagesContainerViewModel>
            {
                new MyMessagesContainerViewModel
                {
                    Messages = new ObservableCollection<MessageViewModel>
                    {
                        new MyMessageViewModel
                        {
                            UserLogin = "kibnet",
                            Text = "Привет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\n",
                            PostTime = DateTimeOffset.Now.AddDays(-1),
                            Id = "1",
                            ShowLogin = true,
                        }
                        ,new MyMessageViewModel
                        {
                            UserLogin = "kibnet",
                            Text = "Есть кто?",
                            PostTime = DateTimeOffset.Now,
                            Id = "2"
                        }
                    }
                },
                new UserMessagesContainerViewModel
                {
                    Messages = new ObservableCollection<MessageViewModel>
                    {
                        new UserMessageViewModel
                        {
                            UserLogin = "alice",
                            Text = "Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? ",
                            PostTime = DateTimeOffset.Now.AddDays(-1),
                            Id = "1",
                            ShowLogin = true,
                        }
                        ,new UserMessageViewModel
                        {
                            UserLogin = "alice",
                            Text = "Есть кто?",
                            PostTime = DateTimeOffset.Now,
                            Id = "2"
                        }
                    }
                },

                new MyMessagesContainerViewModel
                {
                    Messages = new ObservableCollection<MessageViewModel>
                    {
                        new MyMessageViewModel
                        {
                            UserLogin = "kibnet",
                            Text = "Привет, как дела?",
                            PostTime = DateTimeOffset.Now.AddDays(-1),
                            Id = "1",
                            ShowLogin = true,
                        }
                        ,new MyMessageViewModel
                        {
                            UserLogin = "kibnet",
                            Text = "Есть кто?",
                            PostTime = DateTimeOffset.Now,
                            Id = "2"
                        }
                    }
                },
                new UserMessagesContainerViewModel
                {
                    Messages = new ObservableCollection<MessageViewModel>
                    {
                        new UserMessageViewModel
                        {
                            UserLogin = "alice",
                            Text = "Привет, как дела?",
                            PostTime = DateTimeOffset.Now.AddDays(-1),
                            Id = "1",
                            ShowLogin = true,
                        }
                        ,new UserMessageViewModel
                        {
                            UserLogin = "alice",
                            Text = "Есть кто?",
                            PostTime = DateTimeOffset.Now,
                            Id = "2"
                        }
                    }
                },

                new UserMessagesContainerViewModel
                {
                    Messages = new ObservableCollection<MessageViewModel>
                    {
                        new UserMessageViewModel
                        {
                            UserLogin = "bob",
                            Text = "Привет, как дела?",
                            PostTime = DateTimeOffset.Now,
                            Id = "1",
                            ShowLogin = true,
                        }
                    }
                }
            };
        }
    }
}