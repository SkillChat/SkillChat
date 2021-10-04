using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using PropertyChanged;
using ReactiveUI;
using ServiceStack;
using Splat;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using SkillChat.Client.ViewModel.Interfaces;
using SkillChat.Interface;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class SelectMessages
    {
        private IChatHub _hub;
        /// <summary>
        /// Переменная - флаг для вкл./выкл. режима выбора сообщений
        /// </summary>
        public bool IsTurnedSelectMode { get; set; }

        /// <summary>
        /// Счётчик выбранных сообщений
        /// </summary>
        public int CountCheckedMsg { get; set; }

        /// <summary>
        /// Коллекция для временного (оперативного) хранения выбранных сообщений
        /// </summary>
        public ObservableCollection<MessageViewModel> SelectedCollection { get; set; }

        public SelectMessages()
        {
            MainWindowViewModel = Locator.Current.GetService<MainWindowViewModel>();

            SelectedCollection = new ObservableCollection<MessageViewModel>();

            // При изменении SelectedCollection - изменяется счётчик CountCheckedMsg
            SelectedCollection.CollectionChanged += (sender, args) => CountCheckedMsg = SelectedCollection.Count;

            CopyToClipboardCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                StringBuilder text = new StringBuilder();
                var sortByDateMessage = SelectedCollection.OrderBy(m => m.Time);

                foreach (var message in sortByDateMessage)
                {
                    string txt = $"{message.UserNickname}\n {message.Text}\n {message.Time}\n";
                    text.Append(txt);
                }

                var clipboard = Locator.Current.GetService<IClipboard>();
                await clipboard.SetTextAsync(text.ToString());

                CheckOff();
                MainWindowViewModel.MessageCleaningViewModel.Close();
            });

            TurnOffSelectModeCommand = ReactiveCommand.CreateFromTask(async () => { await Task.Run(CheckOff); });
        }

        public void Select(MessageViewModel item)
        {
            SelectedCollection.Add(item);
        }

        public void UnSelect(MessageViewModel item)
        {
            SelectedCollection.Remove(item);
        }

        /// <summary>
        /// Метод переводит чек боксы выбранных сообщений в false и очищает коллекцию выбранных сообщений
        /// </summary>
        public void CheckOff()
        {
            foreach (var item in SelectedCollection.ToList())
            {
                item.IsChecked = false;
            }

            SelectedCollection.Clear();
            IsTurnedSelectMode = false;
        }

        /// <summary>
        /// Команда вызывается из SelectMessageBorderControl. Копирует NickName, Text, Time выбранных сообщений в буфер обмена
        /// </summary>
        public ICommand CopyToClipboardCommand { get; }

        /// <summary>
        /// Команда для выхода из режима выбора сообщений
        /// </summary>
        public ICommand TurnOffSelectModeCommand { get; }

        /// <summary>
        /// Метод включает режим выбора сообщений из контектного меню, вызываемого кнопкой "..." на панели главного окна приложения
        /// </summary>
        public void SelectModeOn()
        {
            IsTurnedSelectMode = true;
            MainWindowViewModel.SettingsViewModel.CloseContextMenu();
        }

        public MainWindowViewModel MainWindowViewModel { get; set; }
        public void SetChatHub(IChatHub chatHub) => _hub = chatHub;
    }
}