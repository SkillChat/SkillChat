using AppAutomation.Avalonia.Headless.Session;
using Avalonia.Headless;
using SkillChat.AppAutomation.TestHost;
using TUnit.Core;

namespace SkillChat.UiTests.Headless.Infrastructure;

public static class HeadlessSessionHooks
{
    private static HeadlessUnitTestSession? _session;

    [Before(TestSession)]
    public static void SetupSession()
    {
        _session = HeadlessUnitTestSession.StartNew(SkillChatAppLaunchHost.AvaloniaAppType);
        HeadlessRuntime.SetSession(_session);
    }

    [After(TestSession)]
    public static void CleanupSession()
    {
        HeadlessRuntime.SetSession(null);
        _session?.Dispose();
        _session = null;
    }
}
