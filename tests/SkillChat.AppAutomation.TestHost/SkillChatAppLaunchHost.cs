using System;
using System.Collections.Generic;
using System.Text.Json;
using AppAutomation.Session.Contracts;
using AppAutomation.TestHost.Avalonia;
using SkillChat.Client;

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

    public static DesktopAppLaunchOptions CreateDesktopLaunchOptions(string? buildConfiguration = null)
    {
        var workspace = Workspace.Value;

        return AvaloniaDesktopLaunchHost.CreateLaunchOptions(
            DesktopApp,
            new AvaloniaDesktopLaunchOptions
            {
                BuildConfiguration = buildConfiguration ?? BuildConfigurationDefaults.ForAssembly(typeof(SkillChatAppLaunchHost).Assembly),
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    [SkillChatClientBootstrap.SettingsPathEnvironmentVariable] = workspace.SettingsFilePath
                }
            });
    }

    public static HeadlessAppLaunchOptions CreateHeadlessLaunchOptions()
    {
        return AvaloniaHeadlessLaunchHost.Create(
            static () => SkillChatClientBootstrap.CreateMainWindow(),
            static _ =>
            {
                Workspace.Value.InitializeCurrentProcess();
                return ValueTask.CompletedTask;
            });
    }

    private static AutomationWorkspace CreateWorkspace()
    {
        var temporaryDirectory = TemporaryDirectory.Create("SkillChat-AppAutomation");
        var attachmentDirectory = temporaryDirectory.CreateDirectory("Attachments");
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

        AppDomain.CurrentDomain.ProcessExit += (_, _) => temporaryDirectory.Dispose();

        return new AutomationWorkspace(temporaryDirectory, settingsFilePath);
    }

    private sealed class AutomationWorkspace
    {
        public AutomationWorkspace(TemporaryDirectory directory, string settingsFilePath)
        {
            Directory = directory;
            SettingsFilePath = settingsFilePath;
        }

        private TemporaryDirectory Directory { get; }
        public string SettingsFilePath { get; }

        public void InitializeCurrentProcess()
        {
            SkillChatClientBootstrap.InitializeServices(SettingsFilePath);
        }
    }
}
