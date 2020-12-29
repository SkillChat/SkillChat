using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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

        /// <summary>Переопределение стандартного поеведения по нажатиям на клавиши клавиатуры у поля ввода сообщений</summary>
        /// <param name="sender">Инициатор (ТекстБокс)</param>
        /// <param name="e">Переданные параметры</param>
		public void InputMessageTB_KeyDown(object sender, KeyEventArgs e)
		{
            var textBox = sender as TextBox;
            if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.Control)
			{
                textBox.Text += Environment.NewLine;
			}
        }
	}
}
