using System;
using System.Threading;
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
        public MainWindow()
        {
            InitializeComponent();
			MessagesScroller = this.Get<ScrollViewer>("MessagesSV");
            this.DataContextChanged += SetDataContextMethod;
#if DEBUG
            this.AttachDevTools();
#endif
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
                vm.MessageReceived += async receivedMessageArgs =>
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (receivedMessageArgs.Message is UserMessageViewModel userMessage)
                        {
                            MessageStatusesSetter.SetReceived(userMessage);
                        }
                        else if (receivedMessageArgs.Message is MyMessageViewModel myMessage)
                        {
                            MessageStatusesSetter.SetSended(myMessage);
                        }
                    });
                };
            }
		}

        /// <summary>При изменении размеров ScrollView скролит его вниз</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollToEndMethod(object sender, AvaloniaPropertyChangedEventArgs e)
		{
			if (e.Property.PropertyType.Name == "Size")
				MessagesScroller.ScrollToEnd();
            MessagesScroller.PropertyChanged -= ScrollToEndMethod;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

    
    }
}
