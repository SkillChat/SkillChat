using TUnit.Core;

namespace SkillChat.Server.Test.TestInfrastructure;

internal static class TestEnvironment
{
    public static ServerTestHost Host { get; private set; } = null!;

    [Before(Assembly)]
    public static async Task StartAsync()
    {
        Host = await ServerTestHost.StartAsync();
    }

    [After(Assembly)]
    public static async Task StopAsync()
    {
        if (Host is not null)
        {
            await Host.DisposeAsync();
        }
    }
}
