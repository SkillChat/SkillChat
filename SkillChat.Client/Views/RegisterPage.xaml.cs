﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SkillChat.Client.ViewModel;

namespace SkillChat.Client.Views
{
    public class RegisterPage : UserControl
    {
        public RegisterPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
