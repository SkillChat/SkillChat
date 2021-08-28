using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
#if DEBUG
            this.AttachDevTools();
#endif
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
                var verticaloffsetvalue = ScrollViewer.VerticalScrollBarValueProperty.Getter(MessagesScroller);
                var verticaloffsetmax = ScrollViewer.VerticalScrollBarMaximumProperty.Getter(MessagesScroller);
                ViewModel.SettingsViewModel.AutoScroll = verticaloffsetmax.Equals(verticaloffsetvalue);

                if (verticaloffsetmax != 0)
                {
                    if (ViewModel.IsFirstRun)
                    {
                        // можно задать нужную позицию
                        MessagesScroller.ScrollToEnd();
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

        private double LastVerticaloffsetmax { get; set; }
        private bool isLoaded = true;

        public void LayoutUpdated_window(object sender, EventArgs e)
        {
            if (sender is MainWindow window && ViewModel != null)
            {
                ViewModel.windowIsFocused = window.IsFocused;
            }
        }

        /// <summary>Контрол скролла для списка сообщений</summary>
        ScrollViewer MessagesScroller;

        private void MessagesControlOnLayoutUpdated(object? sender, EventArgs e)
        {
            if (ViewModel != null)
            {
                MessagesScroller.LayoutUpdated -= MessagesControlOnLayoutUpdated;
                var verticaloffsetmax = ScrollViewer.VerticalScrollBarMaximumProperty.Getter(MessagesScroller);
                MessagesScroller.Offset =
                    new Vector(MessagesScroller.Offset.X,
                        verticaloffsetmax - LastVerticaloffsetmax);

                isLoaded = true;
            }
        }

        /// <summary>Устанавливаем текущий датаконтекст</summary>
        private void SetDataContextMethod(object sender, EventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.MessageReceived += x => MessagesScroller.PropertyChanged += ScrollMethod;
            }
        }

        /// <summary>При изменении размеров ScrollView скролит его вниз если разешено</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void ScrollMethod(object sender, AvaloniaPropertyChangedEventArgs e)
        {

            if (e.Property.PropertyType.Name == "Size")
                if (ViewModel != null)
                    if (ViewModel.SettingsViewModel.AutoScroll)
                        MessagesScroller.ScrollToEnd();
            MessagesScroller.PropertyChanged -= ScrollMethod;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
