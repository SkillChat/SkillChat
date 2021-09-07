using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace SkillChat.Client.ViewModel
{
    public class RelayCommand<T> : ICommand
    {
        readonly Action<T> _execute;
        readonly Func<T, bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public void RefreshCommand()
        {
            var cec = CanExecuteChanged;
            if (cec != null)
                cec(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null) return true;
            return _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }
    }

    public class RelayCommand : RelayCommand<object>
    {
        public RelayCommand(Action execute, Func<bool> canExecute = null)
            : base(_ => execute(),
                _ => canExecute == null || canExecute())
        {

        }
    }
}
