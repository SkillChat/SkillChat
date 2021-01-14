using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using Avalonia.Input;

namespace SkillChat.Client.Utils
{
    
   public class KeyConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var typeKey = (bool) value;
            return typeKey ? new KeyGesture(Key.Enter) : new KeyGesture(Key.Enter,KeyModifiers.Control);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return true;
        }
    }
}
