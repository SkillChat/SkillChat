﻿using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using SkillChat.Client.Utils;
using SkillChat.Client.ViewModel;
namespace SkillChat.Client.Views
{
    public class MainWindow : Window, IHaveWidth, IHaveIsActive
    {
        private MessageStatusesSetter statusSetter;
        private ItemsControl messagesList;
        public MainWindow()
        {
            InitializeComponent();
			MessagesScroller = this.Get<ScrollViewer>("MessagesSV");
            messagesList = this.Get<ItemsControl>("MessagesList");
            MessagesScroller.ScrollChanged += MessagesScroller_ScrollChanged; 
            this.DataContextChanged += SetDataContextMethod;
            Observable.FromEventPattern<ScrollChangedEventArgs>(
                    handler => MessagesScroller.ScrollChanged += handler,
                    handler => MessagesScroller.ScrollChanged -= handler)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(async _ => await Dispatcher.UIThread.InvokeAsync(() => statusSetter.SetRead(MessagesScroller, messagesList)));
#if DEBUG
            this.AttachDevTools();
#endif
        }
        /// <summary>
        /// при изменение прокрутки проверят прокрученно ли до конца и сохраняет во ViewModel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MessagesScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
              var verticaloffsetvalue = ScrollViewer.VerticalScrollBarValueProperty.Getter(MessagesScroller);
              var verticaloffsetmax = ScrollViewer.VerticalScrollBarMaximumProperty.Getter(MessagesScroller);
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
                vm.MessageReceived += x => MessagesScroller.PropertyChanged += ScrollMethod;
                statusSetter ??= new MessageStatusesSetter(this);
            }
		}

        /// <summary>При изменении размеров ScrollView скролит его вниз если разешено</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void ScrollMethod(object sender, AvaloniaPropertyChangedEventArgs e)
		{

			if (e.Property.PropertyType.Name == "Size")
                if(DataContext is MainWindowViewModel vm)
                    if(vm.SettingsViewModel.AutoScroll)
                        MessagesScroller.ScrollToEnd();
            MessagesScroller.PropertyChanged -= ScrollMethod;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

    
    }
}
