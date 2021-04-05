using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using SkillChat.Client.ViewModel;
using System.Reflection;
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

        private void MessagesScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {

                var t = MessagesScroller.GetType();

                PropertyInfo prop = t.GetProperty("VerticalScrollBarValue", BindingFlags.NonPublic | BindingFlags.Instance);
                var verticaloffsetvalue = (double)prop.GetValue(MessagesScroller);
                prop = t.GetProperty("VerticalScrollBarMaximum", BindingFlags.NonPublic | BindingFlags.Instance);
                var verticaloffsetmax = (double)prop.GetValue(MessagesScroller);

                vm.SettingsViewModel.AutoScroll = verticaloffsetmax.Equals(verticaloffsetvalue); 
            }
        }

        public void LayoutUpdated_window(object sender, EventArgs e)
        {
            if (sender is MainWindow window && DataContext is MainWindowViewModel dataContext)
            {
                dataContext.windowIsFocused = window.IsFocused;
            }
        }

        /// <summary>Контрол скролла для списка сообщений</summary>
        ScrollViewer MessagesScroller;

        /// <summary>Устонавливаем текущий датаконтрекст</summary>
        private void SetDataContextMethod(object sender, EventArgs e)
		{
			if (this.DataContext is MainWindowViewModel vm)
			{
                vm.MessageReceived += x => MessagesScroller.PropertyChanged += ScrollToEndMethod;
			}
		}

        /// <summary>При изменении размеров ScrollView скролит его вниз</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void ScrollToEndMethod(object sender, AvaloniaPropertyChangedEventArgs e)
		{

			if (e.Property.PropertyType.Name == "Size")
                if(DataContext is MainWindowViewModel vm)
                    if(vm.SettingsViewModel.AutoScroll)
                        MessagesScroller.ScrollToEnd();
            MessagesScroller.PropertyChanged -= ScrollToEndMethod;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

    
    }
}
