using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ReactiveUI;
using SkillChat.Client.ViewModel;

namespace SkillChat.Client.Views
{
    public class MainWindow : Window, IHaveWidth, IHaveIsActive
    {
        public MainWindow()
        {
            InitializeComponent();
            MessagesScroller = this.Get<ScrollViewer>("MessagesSV");
            MessagesScroller.ScrollChanged += MessagesScroller_ScrollChanged;
            this.DataContextChanged += SetDataContextMethod;
            MessagesScroller.ObservableForProperty(m => m.Viewport.Height)
                .Subscribe(change => Dispatcher.UIThread.Post(() => ViewportHeightEvent(change.Value)));
#if DEBUG
            this.AttachDevTools();
#endif
        }

        /// <summary>
        /// Метод, который при изменении высоты прижимает контент к нижнему краю. 
        /// </summary>
        /// <param name="Height"></param>
        private void ViewportHeightEvent(double Height)
        {
            var deltaHeight = CurrentHeight - Height;
            MessagesScroller.Offset += new Vector(0, deltaHeight);
            CurrentHeight = Height;
        }

        MainWindowViewModel ViewModel => DataContext as MainWindowViewModel;

        /// <summary>
        /// при изменение прокрутки проверят прокрученно ли до конца и сохраняет во ViewModel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MessagesScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                var verticaloffsetvalue = MessagesScroller.Offset.Y;
                var verticaloffsetmax = Math.Max(0, MessagesScroller.Extent.Height - MessagesScroller.Viewport.Height);
                ViewModel.SettingsViewModel.AutoScroll = verticaloffsetvalue >= verticaloffsetmax;
                if (!_suppressNextReadMarkerUpdate && ViewModel.SettingsViewModel.AutoScroll)
                {
                    _ = ViewModel.TryMarkChatReadAsync();
                }
                _suppressNextReadMarkerUpdate = false;

                if (verticaloffsetmax != 0)
                {
                    if (ViewModel.IsFirstRun)
                    {
                        CurrentHeight = MessagesScroller.Viewport.Height;
                        if (ViewModel.HasPendingInitialUnreadBoundaryPositioning)
                        {
                            MessagesScroller.LayoutUpdated += PositionUnreadDividerOnLayoutUpdated;
                        }
                        else
                        {
                            MessagesScroller.ScrollToEnd();
                        }
                        ViewModel.IsFirstRun = false;
                    }
                    else if (verticaloffsetvalue.Equals(0))
                    {
                        if (LastVerticaloffsetmax != verticaloffsetmax && isLoaded)
                        {
                            LastVerticaloffsetmax = verticaloffsetmax;
                            MessagesScroller.LayoutUpdated += MessagesControlOnLayoutUpdated;
                            isLoaded = false;
                            ViewModel.LoadMessageHistoryCommand.Execute(null);
                        }
                    }
                }
            }
        }

        private double CurrentHeight { get; set; }
        private double LastVerticaloffsetmax { get; set; }
        private bool isLoaded = true;
        private bool _suppressNextReadMarkerUpdate;

        public void LayoutUpdated_window(object sender, EventArgs e)
        {
            if (sender is MainWindow window && ViewModel != null)
            {
                ViewModel.windowIsFocused = window.IsFocused;
            }
        }

        /// <summary>Контрол скролла для списка сообщений</summary>
        ScrollViewer MessagesScroller;

        private void MessagesControlOnLayoutUpdated(object sender, EventArgs e)
        {
            if (ViewModel != null)
            {
                MessagesScroller.LayoutUpdated -= MessagesControlOnLayoutUpdated;
                var verticaloffsetmax = Math.Max(0, MessagesScroller.Extent.Height - MessagesScroller.Viewport.Height);
                MessagesScroller.Offset =
                    new Vector(MessagesScroller.Offset.X,
                        verticaloffsetmax - LastVerticaloffsetmax);

                isLoaded = true;
            }
        }

        private void PositionUnreadDividerOnLayoutUpdated(object sender, EventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            var targetMessageId = ViewModel.InitialUnreadBoundaryMessageId;
            if (string.IsNullOrWhiteSpace(targetMessageId))
            {
                MessagesScroller.LayoutUpdated -= PositionUnreadDividerOnLayoutUpdated;
                return;
            }

            var targetMessageControl = this
                .GetVisualDescendants()
                .OfType<Messages>()
                .FirstOrDefault(control =>
                    control.DataContext is MessageViewModel message &&
                    string.Equals(message.Id, targetMessageId, StringComparison.Ordinal));

            if (targetMessageControl == null)
            {
                return;
            }

            MessagesScroller.LayoutUpdated -= PositionUnreadDividerOnLayoutUpdated;

            var messagePosition = targetMessageControl.TranslatePoint(new Point(0, 0), MessagesScroller);
            if (messagePosition is Point point)
            {
                var maxOffset = Math.Max(0, MessagesScroller.Extent.Height - MessagesScroller.Viewport.Height);
                var targetOffset = Math.Clamp(MessagesScroller.Offset.Y + point.Y - 16, 0, maxOffset);
                _suppressNextReadMarkerUpdate = true;
                MessagesScroller.Offset = new Vector(MessagesScroller.Offset.X, targetOffset);
            }

            ViewModel.CompleteInitialUnreadBoundaryPositioning();
        }

        /// <summary>Устанавливаем текущий датаконтекст</summary>
        private void SetDataContextMethod(object sender, EventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.MessageReceived += x => MessagesScroller.PropertyChanged += ScrollMethod;
                
                // Wire up theme toggle callback
                ViewModel.SettingsViewModel.OnThemeChanged = (isDark) => App.SetThemeVariant(isDark);
                // Initialize theme from current setting
                App.SetThemeVariant(ViewModel.SettingsViewModel.IsDarkTheme);
            }
        }

        /// <summary>При изменении размеров ScrollView скролит его вниз если разешено</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void ScrollMethod(object sender, AvaloniaPropertyChangedEventArgs e)
        {

            if (e.Property.PropertyType.Name == "Size")
                if (ViewModel != null)
                {
                    if (ViewModel.SettingsViewModel.AutoScroll || ViewModel.AutoScrollWhenSendingMyMessage)
                        MessagesScroller.ScrollToEnd();
                }
            MessagesScroller.PropertyChanged -= ScrollMethod;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
