using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SkillChat.Client.ViewModel;
using System;
using System.Collections.Generic;

namespace SkillChat.Client.Views
{
    public class LoginPage : UserControl
    {
        public LoginPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
