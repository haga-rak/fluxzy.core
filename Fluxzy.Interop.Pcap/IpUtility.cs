// Copyright © 2022 Haga Rakotoharivelo

using System.Net;
using System.Net.Sockets;

namespace Fluxzy.Interop.Pcap
{
    public static class IpUtility
    {
        private static IPAddress?  _result; 

        public static IPAddress GetDefaultRouteV4Address()
        {
            if (_result != null)
                return _result;

            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.IP);

                socket.Connect("8.8.8.8", 65530);

                IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;
                _result = endPoint?.Address;

                return _result ?? IPAddress.Loopback;
            }
            catch (SocketException)
            {
                return IPAddress.Loopback;
            }
        }
        public static IPEndPoint GetFreeEndpoint()
        {
            var boundIp = IpUtility.GetDefaultRouteV4Address();
            var tcpListener = new TcpListener(boundIp, 0);

            tcpListener.Start();
            var port = ((IPEndPoint) tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();

            return new IPEndPoint(boundIp, port);
        }
    }
}