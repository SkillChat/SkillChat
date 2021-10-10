using System.Windows.Input;
using PropertyChanged;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class MessageCleaningViewModel
    {
        public void Open(ICommand command)
        {
            ConfirmSelectionCommand = command;
            Init?.Invoke();
            IsOpened = !IsOpened;
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
        }

        public void DataForClean()
        {
            ConfirmationQuestion = "Очистить у себя всю историю чата?";
            ButtonName = "Очистить";
        }

        public delegate void InitData();
        public event InitData Init;

        public ICommand ConfirmSelectionCommand { get; set; }
        public bool IsOpened { get; set; }
        public string ConfirmationQuestion { get; set; }
        public string ButtonName { get; set; }
    }
}
