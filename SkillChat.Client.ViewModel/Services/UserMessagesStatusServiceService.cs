using System;
using System.Reactive.Linq;
using AutoMapper;
using SkillChat.Client.ViewModel.Models;
using SkillChat.Interface;
using Splat;

namespace SkillChat.Client.ViewModel.Services
{
    public class UserMessagesStatusServiceService : IUserMessagesStatusService
    {
        private UserMessageStatusModel model = new UserMessageStatusModel();
        private Action<HubUserMessageStatus> sendMethod = null;
        private object readLock = new object();
        private object receiveLock = new object();
        private IMapper mapper;
        private event Action statusUpdated;

        public UserMessagesStatusServiceService()
        {
            //Магия реактивных штук! Тут происходит подписка на событие добавленя статуса сообщения,
            //если статусы добавляются быстро (быстрее чем 0,2 сек), то они копятся в словаре.
            //Когда после добавления статуса проходит 0,2 сек, из словаря берется коллекция статусов и отправляется на сервер.
            var obs = Observable.FromEvent(
                    handler => statusUpdated += handler,
                    handler => statusUpdated -= handler)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .Subscribe(_ => sendMethod?.Invoke(mapper.Map<HubUserMessageStatus>(model)));
        }

        public void UpdateReadedStatus(string id, DateTimeOffset date)
        {
            if (model.LastReadedMessageDate < date)
            {
                lock (readLock)
                {
                    model.LastReadedMessageId = id;
                    model.LastReadedMessageDate = date;
                    statusUpdated?.Invoke();
                }
            }
        }
        public void UpdateReceivedStatus(string id, DateTimeOffset date)
        {
            if (model.LastReceivedMessageDate < date)
            {
                lock (receiveLock)
                {
                    model.LastReadedMessageId = id;
                    model.LastReceivedMessageDate = date;
                    statusUpdated?.Invoke();
                }
            }
        }
        public void SetSendStatusMethod(Action<HubUserMessageStatus> sendMethod) => this.sendMethod = sendMethod;
        public void LoadUserStatus(UserMessageStatusModel m) => model = m;

    }
}