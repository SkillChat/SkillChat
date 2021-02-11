using System;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace SkillChat.Client.Utils
{
    public class GridLengthValueConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var width = System.Convert.ToString(value);
            return GridLength.Parse(width);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
