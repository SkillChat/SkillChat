using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace SkillChat.Client.Utils
{
    public class BoolToObjectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool) value) return parameter;
            return AvaloniaProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
