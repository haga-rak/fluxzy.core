using System.Net;
using System.Net.Sockets;
using Fluxzy;
using Fluxzy.Core;

namespace Samples.No024.BindUpstreamSocket
{
    internal class Program
    {
        /// <summary>
        /// Shows how to configure every upstream socket before it connects. Typical uses are pinning the
        /// egress interface (IP_UNICAST_IF on Windows, SO_BINDTODEVICE on Linux), a source bind, or
        /// keep-alive tuning. The callback runs once per upstream socket, before connect, and the socket
        /// family matches context.RemoteEndPoint.
        /// </summary>
        static async Task Main()
        {
            var fluxzySetting = FluxzySetting.CreateDefault(IPAddress.Loopback, 44344);

            await using var proxy = new Proxy(fluxzySetting, configureUpstreamSocket: ConfigureSocket);
            var endpoints = proxy.Run();

            using var httpClient = new HttpClient(new HttpClientHandler {
                Proxy = new WebProxy($"http://127.0.0.1:{endpoints.First().Port}"),
                UseProxy = true
            });

            using var response = await httpClient.GetAsync("https://www.example.com/");
            Console.WriteLine($"status {(int) response.StatusCode}");
        }

        static void ConfigureSocket(UpstreamSocketContext context)
        {
            var socket = context.Socket;

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            // Pin the egress interface to a physical NIC. Requires the right interface index and,
            // on Linux, elevated privilege. Branch on the family because the option differs.
            //
            // const int interfaceIndex = 12;
            // if (socket.AddressFamily == AddressFamily.InterNetwork)
            //     socket.SetSocketOption(SocketOptionLevel.IP, (SocketOptionName) 31,
            //         IPAddress.HostToNetworkOrder(interfaceIndex));            // IPv4, network order
            // else
            //     socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName) 31, interfaceIndex); // IPv6, host order
            //
            // Linux SO_BINDTODEVICE:
            // socket.SetRawSocketOption(1, 25, System.Text.Encoding.ASCII.GetBytes("eth0"));

            Console.WriteLine(
                $"upstream {context.RequestedHost}:{context.RequestedPort} via {context.RemoteEndPoint} ({socket.AddressFamily})");
        }
    }
}
