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
        // Telegram-style vibrant avatar colors that work well on both dark (#0E1621) and light (#FFFFFF) backgrounds
        private static string[] colorHexes =
        {
            "#E17076", // Red
            "#7BC862", // Green
            "#E5CA77", // Yellow/Gold
            "#65AADD", // Blue
            "#A695E7", // Purple
            "#EE7AE6", // Pink
            "#6EC9CB", // Teal
            "#FAA774", // Orange
            "#E17076", "#7BC862", "#E5CA77", "#65AADD",
            "#A695E7", "#EE7AE6", "#6EC9CB", "#FAA774"
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
