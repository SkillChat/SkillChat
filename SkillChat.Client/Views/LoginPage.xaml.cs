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
            this.DataContextChanged += ChangeStyles;
            controlsStyles = new List<Control>
            {
                this.Get<Border>("PasswordBr"),
                this.Get<Border>("LoginBr"),
                this.Get<TextBox>("LoginTbx"),
                this.Get<TextBox>("PasswordTB"),
                this.Get<TextBlock>("LoginTB"),
                this.Get<TextBlock>("PasswordTBL")
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
                   foreach(var cont in controlsStyles){ cont.Classes.Set("Error", true);}
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
