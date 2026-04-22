using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AppAutomation.Session.Contracts;
using AppAutomation.TestHost.Avalonia;
using SkillChat.Client.Automation;
using SkillChat.Client;
using SkillChat.Server.ServiceModel.Molds;
using SkillChat.Server.ServiceModel.Molds.Attachment;
using SkillChat.Server.ServiceModel.Molds.Chats;

namespace SkillChat.AppAutomation.TestHost;

public static class SkillChatAppLaunchHost
{
    private static readonly AvaloniaDesktopAppDescriptor DesktopApp = new(
        solutionFileNames: new[]
        {
            "SkillChat.sln"
        },
        desktopProjectRelativePaths: new[]
        {
            "SkillChat.Client\\SkillChat.Client.csproj"
        },
        desktopTargetFramework: "net10.0",
        executableName: "SkillChat.Client.exe");

    private static readonly Lazy<AutomationWorkspace> Workspace = new(CreateWorkspace);

    public static Type AvaloniaAppType => typeof(App);

    public static DesktopAppLaunchOptions CreateDesktopLaunchOptions(
        SkillChatAutomationScenario scenario = SkillChatAutomationScenario.Anonymous,
        string? buildConfiguration = null)
    {
        var workspace = Workspace.Value;
        workspace.SetCurrentProcessEnvironment(scenario);

        return AvaloniaDesktopLaunchHost.CreateLaunchOptions(
            DesktopApp,
            new AvaloniaDesktopLaunchOptions
            {
                BuildConfiguration = buildConfiguration ?? BuildConfigurationDefaults.ForAssembly(typeof(SkillChatAppLaunchHost).Assembly),
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    [SkillChatClientBootstrap.SettingsPathEnvironmentVariable] = workspace.SettingsFilePath,
                    [SkillChatClientBootstrap.AutomationScenarioEnvironmentVariable] = scenario.ToString(),
                    [SkillChatClientBootstrap.AutomationStatePathEnvironmentVariable] = workspace.AutomationStateFilePath
                }
            });
    }

    public static HeadlessAppLaunchOptions CreateHeadlessLaunchOptions(
        SkillChatAutomationScenario scenario = SkillChatAutomationScenario.Anonymous)
    {
        return AvaloniaHeadlessLaunchHost.Create(
            () => SkillChatClientBootstrap.CreateMainWindow(),
            _ =>
            {
                Workspace.Value.InitializeCurrentProcess(scenario);
                return ValueTask.CompletedTask;
            });
    }

    private static AutomationWorkspace CreateWorkspace()
    {
        var temporaryDirectory = TemporaryDirectory.Create("SkillChat-AppAutomation");
        var attachmentDirectory = temporaryDirectory.CreateDirectory("Attachments");
        var pickerDirectory = temporaryDirectory.CreateDirectory("PickerFiles");
        var pickerFilePath = Path.Combine(pickerDirectory, "smoke-attachment.txt");
        var messageAttachmentPath = Path.Combine(attachmentDirectory, "team-plan.txt");
        File.WriteAllText(pickerFilePath, "Smoke attachment from AppAutomation picker.");
        File.WriteAllText(messageAttachmentPath, "Seeded attachment already present in attachment directory.");

        var settingsFilePath = temporaryDirectory.WriteTextFile(
            "Settings.json",
            JsonSerializer.Serialize(
                new
                {
                    ChatClientSettings = new
                    {
                        HostUrl = "http://127.0.0.1:5000",
                        AccessToken = string.Empty,
                        RefreshToken = string.Empty,
                        UserName = string.Empty,
                        Login = string.Empty,
                        AttachmentDefaultPath = attachmentDirectory
                    }
                },
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
        var automationStateFilePath = temporaryDirectory.WriteTextFile(
            "automation-state.json",
            JsonSerializer.Serialize(
                CreateAutomationState(pickerFilePath, messageAttachmentPath),
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));

        AppDomain.CurrentDomain.ProcessExit += (_, _) => temporaryDirectory.Dispose();

        return new AutomationWorkspace(temporaryDirectory, settingsFilePath, automationStateFilePath);
    }

    private static SkillChatAutomationState CreateAutomationState(string pickerFilePath, string messageAttachmentPath)
    {
        var currentUserId = "user-smoke-self";
        var otherUserId = "user-smoke-peer";
        var attachmentId = "attachment-smoke-team-plan";
        var now = DateTimeOffset.UtcNow;
        var messages = new List<MessageMold>
        {
            new()
            {
                Id = "message-peer-1",
                UserId = otherUserId,
                UserNickName = "Alice",
                Text = "Привет, это тестовое сообщение для смоука.",
                PostTime = now.AddMinutes(-18),
                ChatId = "chat-smoke-main"
            },
            new()
            {
                Id = "message-peer-attachment",
                UserId = otherUserId,
                UserNickName = "Alice",
                Text = "Вложила план в чат.",
                PostTime = now.AddMinutes(-16),
                ChatId = "chat-smoke-main",
                Attachments = new List<AttachmentMold>
                {
                    new()
                    {
                        Id = attachmentId,
                        SenderId = otherUserId,
                        FileName = Path.GetFileName(messageAttachmentPath),
                        UploadDateTime = now.AddMinutes(-16),
                        Hash = attachmentId,
                        Size = new FileInfo(messageAttachmentPath).Length
                    }
                }
            },
            new()
            {
                Id = "message-self-quoted",
                UserId = currentUserId,
                UserNickName = "UI Smoke User",
                Text = "Подтверждаю, вижу файл.",
                PostTime = now.AddMinutes(-15),
                ChatId = "chat-smoke-main",
                QuotedMessage = new MessageMold
                {
                    Id = "message-peer-1",
                    UserId = otherUserId,
                    UserNickName = "Alice",
                    Text = "Привет, это тестовое сообщение для смоука.",
                    PostTime = now.AddMinutes(-18),
                    ChatId = "chat-smoke-main"
                }
            }
        };

        for (var index = 0; index < 18; index++)
        {
            messages.Add(new MessageMold
            {
                Id = $"message-unread-{index + 1}",
                UserId = otherUserId,
                UserNickName = "Alice",
                Text = $"Непрочитанное сообщение #{index + 1}",
                PostTime = now.AddMinutes(-14).AddSeconds(index * 20),
                ChatId = "chat-smoke-main"
            });
        }

        return new SkillChatAutomationState
        {
            CurrentUser = new SkillChatAutomationCurrentUser
            {
                Id = currentUserId,
                Login = "smoke-user",
                UserName = "UI Smoke User"
            },
            Tokens = new TokenResult
            {
                AccessToken = "smoke-access-token",
                RefreshToken = "smoke-refresh-token"
            },
            Profile = new UserProfileMold
            {
                Id = currentUserId,
                Login = "smoke-user",
                DisplayName = "UI Smoke User",
                AboutMe = "Детерминированный профиль для AppAutomation smoke."
            },
            Settings = new UserChatSettings
            {
                Id = "settings-smoke-user",
                SendingMessageByEnterKey = true
            },
            LoginAudit = new LoginHistory
            {
                UniqueSessionUser = "session-current",
                History = new List<UserLoginAudit>
                {
                    new()
                    {
                        Id = currentUserId,
                        IpAddress = "127.0.0.1",
                        SessionId = "session-current",
                        NameVersionClient = "SkillChat Avalonia Client 1.0",
                        OperatingSystem = "Windows 11",
                        DateOfEntry = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = currentUserId,
                        IpAddress = "192.168.0.15",
                        SessionId = "session-previous",
                        NameVersionClient = "SkillChat Avalonia Client 0.9",
                        OperatingSystem = "Windows 10",
                        DateOfEntry = DateTime.UtcNow.AddDays(-1)
                    }
                }
            },
            ActiveChat = new ChatMold
            {
                Id = "chat-smoke-main",
                ChatName = "Командный чат",
                OwnerId = currentUserId,
                ChatType = ChatTypeMold.Public,
                Members = new List<ChatMemberMold>()
            },
            MembersCaption = "Вы, Alice, Bob",
            IsDarkTheme = true,
            IsSidebarExpanded = true,
            WindowWidth = 900,
            FirstUnreadMessageId = "message-unread-1",
            FileDialogSelection = new List<string>
            {
                pickerFilePath
            },
            AttachmentFilePaths = new Dictionary<string, string>
            {
                [attachmentId] = messageAttachmentPath
            },
            Messages = messages
        };
    }

    private sealed class AutomationWorkspace
    {
        public AutomationWorkspace(TemporaryDirectory directory, string settingsFilePath, string automationStateFilePath)
        {
            Directory = directory;
            SettingsFilePath = settingsFilePath;
            AutomationStateFilePath = automationStateFilePath;
        }

        private TemporaryDirectory Directory { get; }
        public string SettingsFilePath { get; }
        public string AutomationStateFilePath { get; }

        public void SetCurrentProcessEnvironment(SkillChatAutomationScenario scenario)
        {
            Environment.SetEnvironmentVariable(
                SkillChatClientBootstrap.SettingsPathEnvironmentVariable,
                SettingsFilePath);
            Environment.SetEnvironmentVariable(
                SkillChatClientBootstrap.AutomationScenarioEnvironmentVariable,
                scenario.ToString());
            Environment.SetEnvironmentVariable(
                SkillChatClientBootstrap.AutomationStatePathEnvironmentVariable,
                AutomationStateFilePath);
        }

        public void InitializeCurrentProcess(SkillChatAutomationScenario scenario)
        {
            SetCurrentProcessEnvironment(scenario);
            SkillChatClientBootstrap.InitializeServices(SettingsFilePath);
        }
    }
}
