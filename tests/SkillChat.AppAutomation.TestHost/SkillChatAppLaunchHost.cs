using AppAutomation.Session.Contracts;
using AppAutomation.TestHost.Avalonia;

namespace SkillChat.AppAutomation.TestHost;

public static class SkillChatAppLaunchHost
{
    private static readonly AvaloniaDesktopAppDescriptor DesktopApp = new(
        solutionFileNames: new[]
        {
            "REPLACE_WITH_YOUR_SOLUTION.sln"
        },
        desktopProjectRelativePaths: new[]
        {
            "src\\REPLACE_WITH_YOUR_DESKTOP_PROJECT\\REPLACE_WITH_YOUR_DESKTOP_PROJECT.csproj"
        },
        desktopTargetFramework: "net10.0",
        executableName: "REPLACE_WITH_YOUR_DESKTOP_EXE.exe");

    public static DesktopAppLaunchOptions CreateDesktopLaunchOptions(string? buildConfiguration = null)
    {
        return AvaloniaDesktopLaunchHost.CreateLaunchOptions(
            DesktopApp,
            new AvaloniaDesktopLaunchOptions
            {
                BuildConfiguration = buildConfiguration ?? BuildConfigurationDefaults.ForAssembly(typeof(SkillChatAppLaunchHost).Assembly)
            });
    }

    public static HeadlessAppLaunchOptions CreateHeadlessLaunchOptions()
    {
        return AvaloniaHeadlessLaunchHost.Create(
            static () => throw new NotImplementedException(
                "Reference your Avalonia app and return the root Window instance here."));
    }
}
