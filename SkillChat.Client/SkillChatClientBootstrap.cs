using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using AutoMapper;
using Avalonia.Controls;
using Microsoft.Extensions.Configuration;
using SkillChat.Client.Automation;
using SkillChat.Client.Utils;
using SkillChat.Client.ViewModel;
using SkillChat.Client.ViewModel.Interfaces;
using SkillChat.Client.ViewModel.Services;
using SkillChat.Client.Views;
using SkillChat.Interface;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.ServiceModel.Molds.Attachment;
using Splat;
using WritableJsonConfiguration;

namespace SkillChat.Client
{
    public static class SkillChatClientBootstrap
    {
        public const string SettingsPathEnvironmentVariable = "SKILLCHAT_SETTINGS_PATH";
        public const string AutomationScenarioEnvironmentVariable = "SKILLCHAT_AUTOMATION_SCENARIO";
        public const string AutomationStatePathEnvironmentVariable = "SKILLCHAT_AUTOMATION_STATE_PATH";

        private static readonly JsonSerializerOptions AutomationJsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public static IConfigurationRoot InitializeServices(string? settingsPath = null)
        {
            var resolvedSettingsPath = ResolveSettingsPath(settingsPath);
            IConfigurationRoot configuration = WritableJsonConfigurationFabric.Create(resolvedSettingsPath, false);

            var chatClientSettings = EnsureChatClientSettingsSection(configuration);
            var automationScenario = ResolveAutomationScenario();
            var automationState = LoadAutomationState(automationScenario);

            Locator.CurrentMutable.RegisterConstant(configuration, typeof(IConfiguration));

            var mapper = AppModelMapping.ConfigureMapping();
            Locator.CurrentMutable.Register<IMapper>(() => mapper);

            if (automationScenario == SkillChatAutomationScenario.SignedInSmoke && automationState != null)
            {
                Locator.CurrentMutable.RegisterConstant(automationState);
                Locator.CurrentMutable.RegisterConstant<ISkillChatApiClient>(new SkillChatAutomationApiClient(automationState));
                Locator.CurrentMutable.Register<INotify>(() => new SkillChatAutomationNotifyWindow());
                Locator.CurrentMutable.Register<ICanOpenFileDialog>(() => new SkillChatAutomationFileDialog(automationState));
                Locator.CurrentMutable.Register<IClipboard>(() => new SkillChatAutomationClipboard());
            }
            else
            {
                Locator.CurrentMutable.RegisterConstant<ISkillChatApiClient>(new ServiceStackSkillChatApiClient(chatClientSettings.HostUrl));
                Locator.CurrentMutable.Register<INotify>(() => new NotifyWindow());
                Locator.CurrentMutable.Register<ICanOpenFileDialog>(() => new CanOpenFileDialog());
                Locator.CurrentMutable.Register<IClipboard>(() => new AvaloniaClipboard());
            }

            return configuration;
        }

        public static MainWindow CreateMainWindow()
        {
            var mainWindow = new MainWindow();
            var viewModel = new MainWindowViewModel();

            if (ResolveAutomationScenario() == SkillChatAutomationScenario.SignedInSmoke)
            {
                var automationState = Locator.Current.GetService<SkillChatAutomationState>()
                    ?? LoadAutomationState(SkillChatAutomationScenario.SignedInSmoke)
                    ?? throw new InvalidOperationException("SignedInSmoke scenario requires a loaded automation state.");

                ApplySignedInSmokeState(viewModel, automationState);
            }

            mainWindow.DataContext = viewModel;
            return mainWindow;
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

        private static ChatClientSettings EnsureChatClientSettingsSection(IConfigurationRoot configuration)
        {
            var chatClientSettings = configuration
                .GetSection("ChatClientSettings")
                .Get<ChatClientSettings>();

            if (chatClientSettings == null)
            {
                chatClientSettings = new ChatClientSettings();
                configuration.Set(new { ChatClientSettings = chatClientSettings });
            }

            if (string.IsNullOrWhiteSpace(chatClientSettings.HostUrl))
            {
                chatClientSettings.HostUrl = "http://localhost:5000";
            }

            if (string.IsNullOrWhiteSpace(chatClientSettings.AttachmentDefaultPath))
            {
                chatClientSettings.AttachmentDefaultPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Download");
            }

            return chatClientSettings;
        }

        private static SkillChatAutomationScenario ResolveAutomationScenario()
        {
            var rawValue = Environment.GetEnvironmentVariable(AutomationScenarioEnvironmentVariable);
            return Enum.TryParse<SkillChatAutomationScenario>(rawValue, ignoreCase: true, out var scenario)
                ? scenario
                : SkillChatAutomationScenario.None;
        }

        private static SkillChatAutomationState? LoadAutomationState(SkillChatAutomationScenario scenario)
        {
            if (scenario != SkillChatAutomationScenario.SignedInSmoke)
            {
                return null;
            }

            var rawStatePath = Environment.GetEnvironmentVariable(AutomationStatePathEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(rawStatePath))
            {
                throw new InvalidOperationException(
                    $"{AutomationStatePathEnvironmentVariable} is required for SignedInSmoke.");
            }

            var statePath = Path.GetFullPath(rawStatePath);
            if (!File.Exists(statePath))
            {
                throw new FileNotFoundException(
                    $"Automation state file was not found at '{statePath}'.",
                    statePath);
            }

            var json = File.ReadAllText(statePath);
            return JsonSerializer.Deserialize<SkillChatAutomationState>(json, AutomationJsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize automation state.");
        }

        private static void ApplySignedInSmokeState(MainWindowViewModel viewModel, SkillChatAutomationState state)
        {
            var mapper = Locator.Current.GetService<IMapper>()
                ?? throw new InvalidOperationException("IMapper must be registered before applying automation state.");
            var apiClient = Locator.Current.GetService<ISkillChatApiClient>();
            var orderedMessages = state.Messages.OrderBy(message => message.PostTime).ToList();
            var mappedMessages = MapMessages(orderedMessages, state.CurrentUser.Id, mapper);

            viewModel.Tokens = Clone(state.Tokens);
            viewModel.User.Id = state.CurrentUser.Id;
            viewModel.User.Login = state.CurrentUser.Login;
            viewModel.User.UserName = state.CurrentUser.UserName;
            viewModel.User.Password = string.Empty;
            viewModel.RegisterUser.Clear();
            viewModel.IsConnected = true;
            viewModel.IsSignedIn = true;
            viewModel.IsShowingLoginPage = false;
            viewModel.IsShowingRegisterPage = false;
            viewModel.ChatId = state.ActiveChat.Id;
            viewModel.ChatName = state.ActiveChat.ChatName;
            viewModel.MembersCaption = state.MembersCaption;
            viewModel.Messages = new ObservableCollection<MessageViewModel>(mappedMessages);
            viewModel.SettingsViewModel.ChatSettings = Clone(state.Settings);
            viewModel.SettingsViewModel.TypeEnter = state.Settings.SendingMessageByEnterKey;
            viewModel.SettingsViewModel.IsDarkTheme = state.IsDarkTheme;
            viewModel.IsSidebarExpanded = state.IsSidebarExpanded;
            viewModel.Width(false);
            viewModel.ProfileViewModel.UpdateChatVisibility(state.WindowWidth);
            viewModel.SyncSidebarSelection();

            if (apiClient != null)
            {
                apiClient.BearerToken = state.Tokens.AccessToken;
            }
        }

        private static List<MessageViewModel> MapMessages(
            IReadOnlyCollection<MessageMold> messages,
            string currentUserId,
            IMapper mapper)
        {
            var cache = new Dictionary<string, MessageViewModel>(StringComparer.Ordinal);
            var result = new List<MessageViewModel>();
            MessageViewModel? previousMessage = null;

            foreach (var message in messages)
            {
                var mappedMessage = MapMessage(message, currentUserId, mapper, cache);
                mappedMessage.ShowNickname = previousMessage == null || previousMessage.UserId != mappedMessage.UserId;
                previousMessage = mappedMessage;
                result.Add(mappedMessage);
            }

            return result;
        }

        private static MessageViewModel MapMessage(
            MessageMold message,
            string currentUserId,
            IMapper mapper,
            IDictionary<string, MessageViewModel> cache)
        {
            if (cache.TryGetValue(message.Id, out var cachedMessage))
            {
                return cachedMessage;
            }

            var mappedMessage = mapper.Map<MessageViewModel>(Clone(message));
            mappedMessage.IsMyMessage = string.Equals(message.UserId, currentUserId, StringComparison.Ordinal);
            mappedMessage.Attachments = message.Attachments?
                .Select(attachment =>
                {
                    var mappedAttachment = new AttachmentMessageViewModel(Clone(attachment));
                    mappedAttachment.IsMyMessage = mappedMessage.IsMyMessage;
                    return mappedAttachment;
                })
                .ToList();

            cache[message.Id] = mappedMessage;

            if (message.QuotedMessage != null)
            {
                mappedMessage.QuotedMessage = MapMessage(message.QuotedMessage, currentUserId, mapper, cache);
            }

            return mappedMessage;
        }

        private static T Clone<T>(T value)
        {
            var json = JsonSerializer.Serialize(value, AutomationJsonOptions);
            return JsonSerializer.Deserialize<T>(json, AutomationJsonOptions)
                ?? throw new InvalidOperationException($"Failed to clone value of type {typeof(T).FullName}.");
        }
    }
}
