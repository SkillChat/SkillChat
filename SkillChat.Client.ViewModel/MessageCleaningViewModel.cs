using System;
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
                _hub.CleanChatForMe(MainWindowViewModel.ChatId);
                MainWindowViewModel.EndEditCommand.Execute(null);
                MainWindowViewModel.CancelQuoted();
                MainWindowViewModel.SelectMessagesMode.TurnOffSelectModeCommand.Execute(null);
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
        }

        public void DataForDelete()
        {
            ConfirmationQuestion = "Удалить у себя выбранные сообщения?";
            ButtonName = "Удалить";
            ConfirmSelectionCommand = MainWindowViewModel.SelectMessagesMode.DeleteMessagesCommand;
        }

        public void DataForClean()
        {
            ConfirmationQuestion = "Очистить у себя всю историю чата?";
            ButtonName = "Очистить";
            ConfirmSelectionCommand = CleaningAllCommand;
        }

        public delegate void InitData();
        public ICommand OpenCommand { get; }
        public ICommand CleaningAllCommand { get; }
        public ICommand ConfirmSelectionCommand { get; set; }
        public bool IsOpened { get; set; }
        public string ConfirmationQuestion { get; set; }
        public string ButtonName { get; set; }
        public MainWindowViewModel MainWindowViewModel { get; set; }
        public void SetChatHub(IChatHub chatHub) => _hub = chatHub;
        public event InitData Init;
    }
}
