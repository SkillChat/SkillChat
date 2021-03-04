using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SkillChat.Client.ViewModel
{
    public static class ClientHost
    {
        public static string GetIpAddress(string ServerURL)
        {
            string ipAddress;

            Uri server = new Uri(ServerURL);

            // тут стоит обобщить и искать не только ip4 адресс см AddressFamily.InterNetwork
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect(server.Host, server.Port);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                ipAddress = endPoint.Address.ToString();
            }
            return ipAddress;
        }
    }
}
