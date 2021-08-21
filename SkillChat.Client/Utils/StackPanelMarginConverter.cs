using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using SkillChat.Client.ViewModel;

namespace SkillChat.Client.Utils
{
    public class StackPanelMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            (MessageViewModel.StatusMessage)value switch
            {
                MessageViewModel.StatusMessage.MyFirstMessage => new Thickness(20, 17, 0, 3),
                MessageViewModel.StatusMessage.MyNotFirstMessage => new Thickness(20, 0, 0, 3),
                MessageViewModel.StatusMessage.SomeonesFirstMessage => new Thickness(0, 17, 20, 3),
                MessageViewModel.StatusMessage.SomeonesNotFirstMessage => new Thickness(0, 0, 20, 3),
                _ => null
            };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
