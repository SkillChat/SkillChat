using System;
using System.Reactive.Linq;
using AutoMapper;
using SkillChat.Client.ViewModel.Interfaces;
using SkillChat.Client.ViewModel.Models;
using SkillChat.Interface;
using Splat;

namespace SkillChat.Client.ViewModel.Services
{
    public class MessageStatusService : IMessageStatusService
    {
        private MainWindowViewModel vm;
        private IMapper mapper;

        private object readStatusLock;
        private object receivedStatusLock;
        private event Action addNewStatus;

        //NeedHelp Нужно ли было здесь создавать свою модель для статуса? Хотя он никак не отображается и вьюмодель ему не нужна.
        private MessageStatusModel userMessagesStatus;
        private MessageStatusModel chatMessageStatus;

        public MessageStatusService(MainWindowViewModel vm)
        {
            readStatusLock = new object();
            receivedStatusLock = new object();
            userMessagesStatus = new();
            chatMessageStatus = new();
            this.vm = vm;
            this.mapper = mapper = Locator.Current.GetService<IMapper>();

            //Магия реактивных штук! Тут происходит подписка на событие добавленя статуса сообщения,
            //если статусы добавляются быстро (быстрее чем 0,2 сек), то они копятся в словаре.
            //Когда после добавления статуса проходит 0,2 сек, из словаря берется коллекция статусов и отправляется на сервер.
            var obs = Observable.FromEvent(
                    handler => addNewStatus += handler,
                    handler => addNewStatus -= handler)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .Subscribe(async _ 
                    => await vm.SendStatuses(mapper
                        .Map<HubMessageStatus>(userMessagesStatus)));
        }

        public void SetMyIncomingMessagesStatus(MessageStatusModel status)
        {
            lock (readStatusLock)
            {
                lock (receivedStatusLock)
                {
                    _ = userMessagesStatus.Update(status);
                }
            }
        }

        public void ReceivedChatMessageStatus(MessageStatusModel newStatus)
        {
            if (chatMessageStatus.Update(newStatus))
            {
                //needhelp А не замутить ли тут параллельную обработку контейнеров?
                //На случай если контейнеров будет очень много...
                //Как вообще оно будет храниться при 10000 контейнеров?
                foreach (var container in vm.Messages)
                {
                    if (container is MyMessagesContainerViewModel myCont)
                    {
                        foreach (var message in myCont.Messages)
                        {
                            if (message is MyMessageViewModel myMess)
                            {
                                SetStatusToMyMessage(myMess);
                            }
                        }
                    }
                }
            }
        }

        //NeedHelp Кажется не правильно использовал локи((
        public void SetIncomingMessageReadStatus(MessageViewModel message)
        {
            //needhelp Может быть сюда ReaderWriterLock сунуть было, или какой то другой механизм блокировки?
            lock (readStatusLock)
            {
                if (userMessagesStatus.LastReadMessageDate < message.PostTime
                && userMessagesStatus.LastReadMessageId != message.Id)
                    {
                        userMessagesStatus.LastReadMessageDate = message.PostTime;
                        userMessagesStatus.LastReadMessageId = message.Id;
                        addNewStatus?.Invoke();
                    }
            }
        }

        public void SetIncomingMessageReсeivedStatus(MessageViewModel message)
        {
            lock (receivedStatusLock)
            {
                if (message.PostTime > userMessagesStatus.LastReceivedMessageDate
                    && message.Id != userMessagesStatus.LastReceivedMessageId)
                {
                    userMessagesStatus.LastReceivedMessageDate = message.PostTime;
                    userMessagesStatus.LastReceivedMessageId = message.Id;
                    addNewStatus?.Invoke();
                }
            }
        }

        public void SetStatusToMyMessage(MyMessageViewModel message)
        {
            if (message.PostTime <= chatMessageStatus.LastReadMessageDate)
            {
                message.SetRead();
            }
            else if (message.PostTime <= chatMessageStatus.LastReceivedMessageDate)
            {
                message.SetReceived();
            }
            else
            {
                message.SetSended();
            }
        }
    }
}