using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using PropertyChanged;
using ReactiveUI;
using ServiceStack;
using Splat;
using Avalonia;
using Avalonia.Input.Platform;

using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class SelectMessages
    {
        /// <summary>
        /// Переменная - флаг для вкл./выкл. режима выбора сообщений
        /// </summary>
        public bool IsTurnedSelectMode { get; set; }

        public int CountCheckedMsg { get; set; }

        /// <summary>
        /// Коллекция для временного (оперативного) хранения выбранных сообщений
        /// </summary>
        public ObservableCollection<MessageViewModel> SelectedMessagesTempCollection { get; set; }

        public SelectMessages()
        {
            SelectedMessagesTempCollection = new ObservableCollection<MessageViewModel>();

            CountCheckedMsg = 0;

            CopyToClipboardCommand = ReactiveCommand.CreateFromTask( async () =>
            {
                await Task.Run(() =>
                {
                    string text = "";
                    foreach (var message in SelectedMessagesTempCollection)
                    {
                        string txt = $"{message.UserNickname}\n {message.Text}\n {message.Time}\n";
                        text += txt;
                    }
                    AvaloniaLocator.Current.GetService<IClipboard>().SetTextAsync(text);
                });

                CheckOff();
                IsTurnedSelectMode = false;
            });

            TurnOffSelectModeCommand = ReactiveCommand.Create(() =>
            {
                CheckOff();
                IsTurnedSelectMode = false;
            });
        }
       
        /// <summary>
        /// Метод переводит чек боксы выбранных сообщений в false и очищает коллекцию выбранных сообщений
        /// </summary>
        public void CheckOff()
        {
            var mv = Locator.Current.GetService<MainWindowViewModel>();
            foreach (var item in mv.Messages)
            {
                item.IsChecked = false;
            }
            SelectedMessagesTempCollection.Clear();
            CountCheckedMsg = 0;
        }

        /// <summary>
        /// Команда вызывается из SelectMessageBorderControl. Копирует NickName, Text, Time выбранных сообщений в буфер обмена
        /// </summary>
        public ICommand CopyToClipboardCommand { get; }

        /// <summary>
        /// Команда для выхода из режима выбора сообщений
        /// </summary>
        public ICommand TurnOffSelectModeCommand { get; }

    }
}