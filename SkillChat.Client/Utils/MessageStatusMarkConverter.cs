using System;
using System.Globalization;
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
            object resource;
            switch (value)
            {
                case MyMessageViewModel.Statuses.Sended:
                    Application.Current.TryFindResource("SendedMessageMark", out resource);
                    break;
                case MyMessageViewModel.Statuses.Received:
                    Application.Current.TryFindResource("ReceivedMessageMark", out resource);
                    break;
                case MyMessageViewModel.Statuses.Read:
                    Application.Current.TryFindResource("ReadMessageMark", out resource);
                    break;
                default:
                    resource = null;
                    break;
            }
            return resource;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
