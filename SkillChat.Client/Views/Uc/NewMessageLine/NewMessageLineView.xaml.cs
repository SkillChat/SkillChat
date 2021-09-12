using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using SkillChat.Client.ViewModel;

namespace SkillChat.Client.Views.Uc.NewMessageLine
{
	public class NewMessageLineView : UserControl
    {
        public NewMessageLineView()
		{
			this.InitializeComponent(); 
        }
        private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
    }
}
