using System.Net;
using System.Net.Sockets;

namespace SkillChat.Server.Test.TestInfrastructure;

internal static class PortHelper
{
    public static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
