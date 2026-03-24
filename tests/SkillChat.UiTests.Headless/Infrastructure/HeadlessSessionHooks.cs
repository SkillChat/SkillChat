using TUnit.Core;

namespace SkillChat.UiTests.Headless.Infrastructure;

public static class HeadlessSessionHooks
{
    [Before(TestSession)]
    public static void SetupSession()
    {
        // TODO: Start your Avalonia Headless session and register it via HeadlessRuntime.SetSession(...).
    }

    [After(TestSession)]
    public static void CleanupSession()
    {
        // TODO: Dispose the Avalonia Headless session and clear HeadlessRuntime.SetSession(null).
    }
}
