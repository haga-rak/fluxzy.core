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

            // Pin the egress interface to a physical NIC. Needs the interface index (or name) and,
            // on Linux, elevated privilege. Branch on the family because the option differs.
            //
            // const int interfaceIndex = 12; // NetworkInterface.GetIPProperties().Get*Properties().Index
            //
            // Windows IP_UNICAST_IF (IPv4 index in network order, IPv6 index in host order):
            // if (socket.AddressFamily == AddressFamily.InterNetwork)
            //     socket.SetSocketOption(SocketOptionLevel.IP, (SocketOptionName) 31,
            //         IPAddress.HostToNetworkOrder(interfaceIndex));
            // else
            //     socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName) 31, interfaceIndex);
            //
            // macOS IP_BOUND_IF (25) / IPV6_BOUND_IF (125), interface index, no privilege:
            // if (socket.AddressFamily == AddressFamily.InterNetwork)
            //     socket.SetRawSocketOption(0, 25, BitConverter.GetBytes(interfaceIndex));
            // else
            //     socket.SetRawSocketOption(41, 125, BitConverter.GetBytes(interfaceIndex));
            //
            // Linux SO_BINDTODEVICE (interface name, needs CAP_NET_RAW/root):
            // socket.SetRawSocketOption(1, 25, System.Text.Encoding.ASCII.GetBytes("eth0"));

            Console.WriteLine(
                $"upstream {context.RequestedHost}:{context.RequestedPort} via {context.RemoteEndPoint} ({socket.AddressFamily})");
        }
    }
}
