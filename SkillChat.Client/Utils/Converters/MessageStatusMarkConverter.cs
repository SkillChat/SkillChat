using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using SkillChat.Client.ViewModel;

namespace SkillChat.Client.Utils.Converters
{
    public class MessageStatusMarkConverter : IValueConverter
    {
        private readonly Lazy<object> sendedMessageMark = new(() =>
        {
            Application.Current.TryFindResource("SendedMessageMark", out var resource);
            return resource;
        });

        private readonly Lazy<object> receivedMessageMark = new(() =>
        {
            Application.Current.TryFindResource("ReceivedMessageMark", out var resource);
            return resource;
        });

        private readonly Lazy<object> readMessageMark = new(() =>
        {
            Application.Current.TryFindResource("ReadMessageMark", out var resource);
            return resource;
        });

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object resource;
            switch (value)
            {
                case MyMessageViewModel.MessageStatus.Sended:
                    resource = sendedMessageMark.Value;
                    break;
                case MyMessageViewModel.MessageStatus.Received:
                    resource = receivedMessageMark.Value;
                    break;
                case MyMessageViewModel.MessageStatus.Readed:
                    resource = readMessageMark.Value;
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