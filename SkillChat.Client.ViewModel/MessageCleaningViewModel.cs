using System;
using System.Windows.Input;
using PropertyChanged;
using ReactiveUI;
using Splat;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class MessageCleaningViewModel
    {
        public delegate void InitData();
        public MainWindowViewModel MainWindowViewModel;

        public MessageCleaningViewModel()
        {
            MainWindowViewModel = Locator.Current.GetService<MainWindowViewModel>();

            CleaningAllCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                MainWindowViewModel.Messages.Clear();
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

        public ICommand OpenCommand { get; }
        public ICommand CleaningAllCommand { get; }
        public ICommand ConfirmSelectionCommand { get; set; }
        public bool IsOpened { get; set; }
        public event InitData Init;
        public string ConfirmationQuestion { get; set; }
        public string ButtonName { get; set; }

    }
}
