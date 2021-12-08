using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Concurrent;
using System.Globalization;

namespace SkillChat.Client.Utils
{
    class UserIdToBrushConverter : IValueConverter
    {
        private static object o = new();
        private static int colorIndex = 0;
        private static string[] colorHexes =
        {
            "#4169E1", "#008080", "#6A5ACD", "#228B22", "#9400D3", "#708090", "#E9967A", "#DAA520", "#CD5C5C",
            "#9370DB", "#8B0000", "#EE82EE", "#FF4500", "#D2691E", "#006400", "#00008B"
        };

        private static ConcurrentDictionary<string, SolidColorBrush> users = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var userId = value as string;
            if (userId != null)
            {
                if(!users.ContainsKey(userId))
                {
                    lock (o)
                    {
                        var hex = colorHexes[colorIndex];
                        if (colorIndex < colorHexes.Length - 1)
                            colorIndex++;
                        else
                            colorIndex = 0;
                        var color = new SolidColorBrush(Color.Parse(hex));
                        users.GetOrAdd(userId, color);
                        return color;
                    }
                }
                return users[userId];
            }
            return new SolidColorBrush(Color.FromRgb(0,0,0));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
