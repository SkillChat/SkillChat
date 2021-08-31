using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using SkillChat.Client.ViewModel;

namespace SkillChat.Client.Views
{
	public class SendMessageControl : UserControl
    {
        private TextBox messageTextBox;
        public SendMessageControl()
		{
			this.InitializeComponent();
            messageTextBox = this.Get<TextBox>("InputMessageTB");
            messageTextBox.LayoutUpdated += messageTextBoxLayoutUpdated;
        }

        private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

        private void messageTextBoxLayoutUpdated(object sender, EventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                if (Length != 0 && !vm.IsCursorSet)
                {
                    vm.IsCursorSet = true;
                    messageTextBox.CaretIndex = Length;
                }
            }
        }

        private int Length => messageTextBox.Text?.Length ?? 0;
    }
}
