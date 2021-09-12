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
            IsShowingLoginPage = false;
            Tokens = new TokenResult();
            MembersCaption = "Вы, Кристина Петрова, Стас Верещагин, Иван";
            MessageText =
                "Привет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\n";

            Messages = new ObservableCollection<IMessageViewModel>
            {
                new MessageViewModel
                {
                    UserNickname = "kibnet",
                    Text =
                        "Привет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\nПривет, как дела?\n",
                    PostTime = DateTimeOffset.Now.AddDays(-1),
                    Id = "1",
                    ShowNickname = true,
                    IsMyMessage = true
                },
                new MessageViewModel
                {
                    UserNickname = "kibnet",
                    Text = "Есть кто?",
                    PostTime = DateTimeOffset.Now,
                    Id = "2",
                    IsMyMessage = true
                },
                new MessageViewModel()
                {
                    UserNickname = "kibnet",
                    Text = "Есть тут кто-то?",
                    PostTime = DateTimeOffset.Now,
                    LastEditTime = DateTimeOffset.Now,
                    Id = "3",
                    IsMyMessage = true
                },
                new MessageViewModel()
                {
                    UserNickname = "alice",
                    Text =
                        "Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? Привет, как дела? ",
                    PostTime = DateTimeOffset.Now.AddDays(-1),
                    Id = "1",
                    ShowNickname = true,
                    IsMyMessage = false
                },
                new MessageViewModel()
                {
                    UserNickname = "alice",
                    Text = "Есть кто?",
                    PostTime = DateTimeOffset.Now,
                    Id = "2",
                    IsMyMessage = false
                },
                new MessageViewModel
                {
                    UserNickname = "kibnet",
                    Text = "Привет, как дела?",
                    PostTime = DateTimeOffset.Now.AddDays(-1),
                    Id = "1",
                    ShowNickname = true,
                    IsMyMessage = true
                },
                new MessageViewModel
                {
                    UserNickname = "kibnet",
                    Text = "Есть кто?",
                    PostTime = DateTimeOffset.Now,
                    Id = "2",
                    IsMyMessage = true
                },
                new MessageViewModel()
                {
                    UserNickname = "alice",
                    Text = "Привет, как дела?",
                    PostTime = DateTimeOffset.Now.AddDays(-1),
                    Id = "1",
                    ShowNickname = true,
                    IsMyMessage = false,
                },
                new MessageViewModel()
                {
                    UserNickname = "alice",
                    Text = "Есть кто?",
                    PostTime = DateTimeOffset.Now,
                    Id = "2",
                    IsMyMessage = false
                },
                new MessageViewModel()
                {
                    UserNickname = "bob",
                    Text = "Привет, как дела?",
                    PostTime = DateTimeOffset.Now,
                    Id = "1",
                    ShowNickname = true,
                    IsMyMessage = false
                }
            };
        }
    }
}