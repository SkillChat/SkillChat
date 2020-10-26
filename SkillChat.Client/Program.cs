using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Configuration;
using SkillChat.Client.ViewModel;
using Splat;
using WritableJsonConfiguration;

namespace SkillChat.Client
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            //Регистрация подсистемы конфигурации
            IConfigurationRoot configuration = WritableJsonConfigurationFabric.Create("Settings.json", false);
            var section = configuration.GetSection("ChatClientSettings");
            if (section == null)
            {
                configuration.Set(new { ChatClientSettings = new ChatClientSettings()});
            }
            Locator.CurrentMutable.RegisterConstant(configuration, typeof(IConfiguration));

            //Запуск авалонии
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug()
                .UseReactiveUI();
    }
}
