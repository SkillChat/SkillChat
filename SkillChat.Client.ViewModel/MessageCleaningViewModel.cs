using System.Windows.Input;
using PropertyChanged;
using ReactiveUI;
using Splat;

namespace SkillChat.Client.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class MessageCleaningViewModel
    {
        public MessageCleaningViewModel()
        {
            var mainWindowViewModel = Locator.Current.GetService<MainWindowViewModel>();
            CleaningAllCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                mainWindowViewModel.Messages.Clear();
                Close();
            });

            OpenCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                IsOpened = !IsOpened;
            });

        }
        public void Close()
        {
            IsOpened = false;
        }

        public ICommand OpenCommand { get; }
        public ICommand CleaningAllCommand { get; }
        public bool IsOpened { get; set; }
    }
}
