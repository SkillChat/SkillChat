using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System;

namespace SkillChat.Client.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>Переопределение стандартного поеведения по нажатиям на клавиши клавиатуры у поля ввода сообщений</summary>
        /// <param name="sender">Инициатор (ТекстБокс)</param>
        /// <param name="e">Переданные параметры</param>
		private void InputMessageTB_KeyDown(object sender, KeyEventArgs e)
		{
            var textBox = sender as TextBox;
            if (e.Key == Key.Enter)
			{
                if (e.KeyModifiers == KeyModifiers.Control)
				{
                    textBox.Text += Environment.NewLine;
                    return;
                }
			}

		}
	}
}
