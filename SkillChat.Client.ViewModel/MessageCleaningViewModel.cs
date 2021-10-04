using System;
using System.Collections.Generic;
using System.Windows.Input;
using PropertyChanged;
using ReactiveUI;
using SkillChat.Interface;
using Splat;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class MessageCleaningViewModel
    {
        private IChatHub _hub;

        public MessageCleaningViewModel()
        {
            MainWindowViewModel = Locator.Current.GetService<MainWindowViewModel>();

            CleaningAllCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                MainWindowViewModel.Messages.Clear();
                await _hub.CleanChatForMe(MainWindowViewModel.ChatId);
                MainWindowViewModel.EndEditCommand.Execute(null);
                MainWindowViewModel.CancelQuoted();
                MainWindowViewModel.SelectMessagesMode.TurnOffSelectModeCommand.Execute(null);
                Close();
            });

            DeleteMessagesCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                List<string> idDeleteMessages = new List<string>();
                foreach (var item in MainWindowViewModel.SelectMessagesMode.SelectedCollection)
                {
                    idDeleteMessages.Add(item.Id);
                    MainWindowViewModel.Messages.Remove(item);
                }
                await _hub.DeleteForMe(idDeleteMessages);
                MainWindowViewModel.EndEditCommand.Execute(null);
                MainWindowViewModel.CancelQuoted();
                MainWindowViewModel.SelectMessagesMode.CheckOff();
                Close();
            });

            OpenCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                Init?.Invoke();
                IsOpened = !IsOpened;
            });

        }
        public void Close()
        {
            IsOpened = false;
            Init = null;
        }

        public void DataForDelete()
        {
            ConfirmationQuestion = "Удалить у себя выбранные сообщения?";
            ButtonName = "Удалить";
            ConfirmSelectionCommand = DeleteMessagesCommand;
        }

        public void DataForClean()
        {
            ConfirmationQuestion = "Очистить у себя всю историю чата?";
            ButtonName = "Очистить";
            ConfirmSelectionCommand = CleaningAllCommand;
        }

        public delegate void InitData();
        /// <summary>
        /// Комана открывает оконо удаления/очистки сообщений.
        /// </summary>
        public ICommand OpenCommand { get; }

        /// <summary>
        /// Команда очищает всю историю чата для текущего пользователя.
        /// </summary>
        public ICommand CleaningAllCommand { get; }

        /// <summary>
        /// Команда удаляет выбранные сообщения для текущего пользователя.
        /// </summary>
        public ICommand DeleteMessagesCommand { get; }

        /// <summary>
        /// Выбор команды и текста для ее описания 
        /// </summary>
        public ICommand ConfirmSelectionCommand { get; set; }
        public bool IsOpened { get; set; }
        public string ConfirmationQuestion { get; set; }
        public string ButtonName { get; set; }
        public MainWindowViewModel MainWindowViewModel { get; set; }
        public void SetChatHub(IChatHub chatHub) => _hub = chatHub;
        public event InitData Init;
    }
}
