using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using SkillChat.Client.ViewModel;

namespace SkillChat.Client.Utils
{
    public class MessageStatusMarkConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MyMessageViewModel vm)
            {
                object resource;
                if (vm.IsRead && vm.IsReceived && vm.IsSended) Application.Current.TryFindResource("ReadMessageMark", out resource);
                else if (!vm.IsRead && vm.IsReceived && vm.IsSended) Application.Current.TryFindResource("ReceivedMessageMark", out resource);
                else if (!vm.IsRead && !vm.IsReceived && vm.IsSended) Application.Current.TryFindResource("SendedMessageMark", out resource);
                else resource = null;
                return resource;
            }
            throw new ArgumentException("Тип не MyMessageViewModel");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
