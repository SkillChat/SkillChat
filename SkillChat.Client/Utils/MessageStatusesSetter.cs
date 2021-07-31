using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;
using SkillChat.Client.ViewModel;
using SkillChat.Client.ViewModel.Interfaces;
using SkillChat.Client.Views;

namespace SkillChat.Client.Utils
{
    public class MessageStatusesSetter
    {
        private IMessageStatusService statusService;
        public MessageStatusesSetter(MainWindow window)
        {
            //messagesList = window.Get<ItemsControl>("MessagesList");
            //messagesScroller = window.Get<ScrollViewer>("MessagesSV");
            if (window.DataContext is MainWindowViewModel mainVm)
                statusService = mainVm.StatusService;
            else
                statusService = null;
        }
        /// <summary>Установка сообщениям собеседника статуса прочитано</summary>
        /// <param name="messagesList">Список сообщений</param>
        /// <param name="messagesScroller">Контрол отображающий список</param>
        public void SetRead(ScrollViewer messagesScroller, ItemsControl messagesList)
        {
            if (messagesList.ItemCount == 0) return;    // Если в списке нет сообщений, то выходим из метода
            bool lastReadMessageFound = false;          // Если найдено последнее прочитанное сообщение, то тру
            var containersList = messagesList           // Получаем коллекцию визуальных контейнеров из списка сообщений
                .Presenter
                .VisualChildren[0]
                .VisualChildren;

            // Определение границ вьюпорта
            var messageScrollerBottomBound = messagesScroller.TransformedBounds.Value.Transform.M32 +
                                         messagesScroller.Viewport.Height;

            IVisual messageContainer;
            try
            {
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

                        if (messageVisualTopBound + (messageVisual.Bounds.Height-2) < messageScrollerBottomBound)
                        {
                            var messagePresenterContext = (messageVisual as ContentPresenter)?.DataContext;
                            if (messagePresenterContext is UserMessageViewModel message)
                            {
                                if (statusService != null)
                                {
                                    statusService.SetIncomingMessageReadStatus(message);
                                    Debug.WriteLine($"---{DateTime.Now: H:mm:ss ffffff} Последнее видимое сообщение -- '{message.Text}' с id = {message.Id}, " +
                                                   $"добавлено в индекс");
                                    return;
                                }
                                else
                                {
                                    Debug.WriteLine($"---{DateTime.Now : H:mm:ss ffffff} Последнее видимое сообщение -- '{message.Text}' с id = {message.Id}, " +
                                                    $"но сервис статусов сообщение = null!");
                                    return;
                                }                                    
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Error!!!!!---- ", ex.Message);
            }
        }
    }
}