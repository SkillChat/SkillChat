using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using SkillChat.Client.Notification.ViewModels;

namespace SkillChat.Client.Notification
{
    public class Notification
    {
        private static Notification _manager;
        private List<Views.Notify> _notificationWindows = new List<Views.Notify>();


        //Добавление увдеомлений в очередь
        private void Add(Views.Notify window)
        {
            _notificationWindows.Add(window);
            Reposition();
        }

        //Переопределение позиции окна
        private void Reposition()
        {
            int count = 0;
            foreach (var window in _notificationWindows)
            {
                if (!window.IsClosed)
                {
                    count++;
                    window.Position = new PixelPoint(
                        window.Screens.Primary.WorkingArea.BottomRight.X - (int) window.Width,
                        window.Screens.Primary.WorkingArea.BottomRight.Y - count * (int) window.Height - 50 -
                        10 * count);
                }
            }
        }

        public static Notification Manager
        {
            get { return _manager ?? (_manager = new Notification()); }
        }

        //Вызов окна
        public async Task Show(string title, string text, int? timeShow = 10000)
        {
            var w = new Views.Notify();
            w.ShowInTaskbar = false;
            w.Topmost = true;
            w.DataContext = new NotifyViewModel(title, text, w);
            w.Show();
            w.Closed += delegate { Reposition(); };
            Add(w);

            if (timeShow != null)
            {
                await Task.Run(async () =>
                {
                    for (int i = 0; i < timeShow / 100; i++)
                    {
                        await Task.Delay(100);
                        if (w.IsClosed) break;
                    }
                });
                if (!w.IsClosed)
                    w.Close();
            }
        }
    }
}
