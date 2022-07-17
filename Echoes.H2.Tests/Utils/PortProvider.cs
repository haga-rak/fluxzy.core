using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Echoes.H2.Tests.Utils
{
    public static class PortProvider
    {
        private static int _portCounter = Random.Shared.Next(16000, 40000); 

        public static int Next()
        {
            return NextFreeTcpPort(); 
            //return Interlocked.Increment(ref _portCounter); 
        }

        private static int NextFreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}