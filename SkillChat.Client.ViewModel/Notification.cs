using System.Collections.Generic;
using System.Threading.Tasks;
using SkillChat.Client.Notification.ViewModels;
using Splat;

namespace SkillChat.Client.ViewModel
{
    public class Notification
    {
        private static Notification _manager;
        private List<INotify> _notificationWindows = new List<INotify>();


        //Добавление увдеомлений в очередь
        private void Add(INotify window)
        {
            _notificationWindows.Insert(0,window);
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
                    window.SetPosition(window.ScreenBottomRightX - (int)(window.Width*window.PrimaryPixelDensity), 
                        window.ScreenBottomRightY - count * (int)(window.Height* window.PrimaryPixelDensity) - (int)(50 * window.PrimaryPixelDensity) -(int) (10 * window.PrimaryPixelDensity)*count);
                }
            }
        }

        public static Notification Manager
        {
            get { return _manager ?? (_manager = new Notification()); }
        }

        //Вызов окна
        public async void Show(string title, string text, int? timeShow = 10000)
        {
            var w = Locator.Current.GetService<INotify>();
            w.ShowInTaskbar = false;
            w.Topmost = true;
            w.DataContext = new NotifyViewModel(title, text, w);
            w.Show();
            w.OnClosed(Reposition);
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
