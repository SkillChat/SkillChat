using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;
using SkillChat.Client.ViewModel;

namespace SkillChat.Client.Utils
{
    public static class MessageStatusesSetter
    {
        /// <summary>Установка сообщениям собеседника статуса прочитано</summary>
        /// <param name="messagesList">Список сообщений</param>
        /// <param name="messagesScroller">Контрол отображающий список</param>
        public static void SetRead(ItemsControl messagesList, ScrollViewer messagesScroller)
        {
            // Если найдено последнее прочитанное сообщение, то тру
            bool lastReadMessageFound = false;
            // Получаем коллекцию визуальных контейнеров из списка сообщений
            var containersList = messagesList
                .Presenter
                .VisualChildren[0]
                .VisualChildren;

            // Определение границ вьюпорта
            var messageScrollerTopBound = messagesScroller.TransformedBounds.Value.Transform.M32;
            var messageScrollerBottomBound = messagesScroller.TransformedBounds.Value.Transform.M32 +
                                         messagesScroller.Viewport.Height;

            IVisual messageContainer;
            
            for (int i = containersList.Count - 1; i > -1; i--)
            {
                // Если последнее отображаемое сообщение прочитано, то выходим из метода
                if (lastReadMessageFound) return;
                
                // Выделяем детей контейнера
                messageContainer = containersList[i]
                    .VisualChildren[0]
                    .VisualChildren[0]
                    .VisualChildren[0]
                    .VisualChildren[0];

                // Если clip контейнера сообщений = 0, то пропускаем котейнер
                if (messageContainer.TransformedBounds.Value.Clip.IsEmpty) continue;

                // Перебираем детей контейнера (это уже сообщения)
                for (int j = messageContainer.VisualChildren.Count - 1; j > -1; j--)
                {
                    var messageVisual = messageContainer.VisualChildren[j];
                    var messageVisualTopBound = messageVisual.TransformedBounds.Value.Transform.M32;

                    // Если Clip равен нулю, то контейнер сообщений вне зоны видимости 
                    if (messageVisual.TransformedBounds.Value.Clip.IsEmpty) continue;

                    if (messageVisualTopBound < messageScrollerBottomBound)
                    {
                        var messagePresenterContext = (messageVisual as ContentPresenter).DataContext;
                        if (messagePresenterContext is UserMessageViewModel userMessage)
                        {
                            if (!userMessage.Read) // Если сообщение не прочитано, то перебираем дальше и помечаем прочитанными
                            {
                                userMessage.Read = true;
                                Debug.WriteLine($"--- {userMessage.Id} помечено прочитанным!");
                            }
                            else
                            {
                                lastReadMessageFound = true;
                                return;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Установка моим сообщениям статуса доставлено</summary>
        /// <param name="myMessage">Сообщение</param>
        public static void SetSended(MyMessageViewModel myMessage)
        {
            if (!myMessage.IsSended) myMessage.IsSended = true;
        }

        /// <summary>Установка сообщениям собеседника статуса получено</summary>
        /// <param name="userMessage">Сообщение собеседника</param>
        public static void SetReceived(UserMessageViewModel userMessage)
        {
            if (!userMessage.Received) userMessage.Received = true;
        }
        
    }
}
