using System;
using System.Collections.Generic;
using Avalonia;
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
            this.DataContextChanged += ChangeStyles;
            controlsStyles = new List<Control>
            {
                this.Get<Border>("LoginBorder"),
                this.Get<Border>("PasswordBorder"),
                this.Get<Border>("NickNameBorder"),
                this.Get<TextBox>("LoginTextBox"),
                this.Get<TextBox>("PasswordTextBox"),
                this.Get<TextBox>("NickNameTextBox"),
                this.Get<TextBlock>("LoginTextBlock"),
                this.Get<TextBlock>("PasswordTextBlock"),
                this.Get<TextBlock>("NickNameTextBlock")
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>Меняем стили</summary>
        private void ChangeStyles(object sender, EventArgs e)
        {
            if (this.DataContext is MainWindowViewModel vm)
            {
                vm.ErrorBe += () => {
                    foreach (var cont in controlsStyles) { cont.Classes.Set("Error", true); }
                };
                vm.ResetError += () =>
                {
                    foreach (var cont in controlsStyles) { cont.Classes.Remove("Error"); }
                };
            }
        }

        //Коллекция контролов для изменения стилей
        List<Control> controlsStyles;

    }
}
