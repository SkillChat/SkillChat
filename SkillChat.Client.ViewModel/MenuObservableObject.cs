using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace SkillChat.Client.ViewModel
{
    public class MenuObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged<T>(Expression<Func<T>> raiser)
        {
            var propName = ((MemberExpression)raiser.Body).Member.Name;
            OnPropertyChanged(propName);
        }

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(name);
                return true;
            }
            return false;
        }
    }
}
