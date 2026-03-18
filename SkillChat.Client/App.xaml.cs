using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using SkillChat.Client.ViewModel;
using SkillChat.Client.Views;

namespace SkillChat.Client
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Switches the application theme between Dark and Light variants at runtime.
        /// </summary>
        /// <param name="isDark">True for Dark theme, false for Light theme.</param>
        public static void SetThemeVariant(bool isDark)
        {
            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = isDark 
                    ? ThemeVariant.Dark 
                    : ThemeVariant.Light;
            }
        }
    }
}
