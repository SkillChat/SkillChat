using System;
using System.IO;
using AutoMapper;
using Avalonia.Controls;
using Microsoft.Extensions.Configuration;
using SkillChat.Client.Utils;
using SkillChat.Client.ViewModel;
using SkillChat.Client.ViewModel.Interfaces;
using SkillChat.Client.Views;
using SkillChat.Interface;
using Splat;
using WritableJsonConfiguration;

namespace SkillChat.Client
{
    public static class SkillChatClientBootstrap
    {
        public const string SettingsPathEnvironmentVariable = "SKILLCHAT_SETTINGS_PATH";

        public static IConfigurationRoot InitializeServices(string? settingsPath = null)
        {
            var resolvedSettingsPath = ResolveSettingsPath(settingsPath);
            IConfigurationRoot configuration = WritableJsonConfigurationFabric.Create(resolvedSettingsPath, false);

            EnsureChatClientSettingsSection(configuration);

            Locator.CurrentMutable.RegisterConstant(configuration, typeof(IConfiguration));
            Locator.CurrentMutable.Register<INotify>(() => new NotifyWindow());
            Locator.CurrentMutable.Register<ICanOpenFileDialog>(() => new CanOpenFileDialog());
            Locator.CurrentMutable.Register<IClipboard>(() => new AvaloniaClipboard());

            var mapper = AppModelMapping.ConfigureMapping();
            Locator.CurrentMutable.Register<IMapper>(() => mapper);

            return configuration;
        }

        public static MainWindow CreateMainWindow()
        {
            return new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        private static string ResolveSettingsPath(string? settingsPath)
        {
            if (!string.IsNullOrWhiteSpace(settingsPath))
            {
                return Path.GetFullPath(settingsPath);
            }

            var environmentSettingsPath = Environment.GetEnvironmentVariable(SettingsPathEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(environmentSettingsPath))
            {
                return Path.GetFullPath(environmentSettingsPath);
            }

            return "Settings.json";
        }

        private static void EnsureChatClientSettingsSection(IConfigurationRoot configuration)
        {
            var chatClientSettings = configuration
                .GetSection("ChatClientSettings")
                .Get<ChatClientSettings>();

            if (chatClientSettings == null)
            {
                configuration.Set(new { ChatClientSettings = new ChatClientSettings() });
            }
        }
    }
}
