// Copyright © 2022 Haga Rakotoharivelo

using System.Net;
using System.Net.Sockets;

namespace Fluxzy.Interop.Pcap
{
    public static class IpUtility
    {
        private static IPAddress?  _result = null; 

        public static IPAddress GetNetworkAddress()
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
            var boundIp = IpUtility.GetNetworkAddress();
            var tcpListener = new TcpListener(boundIp, 0);

            tcpListener.Start();
            var port = ((IPEndPoint) tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();

            return new IPEndPoint(boundIp, port);
        }
    }

    public class LocalAvailablePortProvider
    {
        private readonly int _startPort;
        private readonly int _endPort;
        private int _current;

        public LocalAvailablePortProvider(int startPort = 25000, int endPort = 65534)
        {
            _startPort = startPort;
            _endPort = endPort;
            _startPort = startPort;
        }

        private void Increment()
        {
            var range = _endPort - _startPort;

            var nextValue = _current + 1;

            if (nextValue >= _endPort)
                nextValue = _startPort;

            _current = nextValue; 
        }



    }
}