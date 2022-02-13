using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.Core
{
    internal class TcpDownStreamConnection : IDownStreamConnection
    {
        private TcpClient _tcpClient;
        private readonly IReferenceClock _referenceClock;
        private static int _incCounter;
        private IHttpStreamReader _httpStreamReader;
        private string _preferedFileName;

        internal TcpDownStreamConnection(
            TcpClient tcpClient, 
            DateTime instantConnectingUtc,
            DateTime instantConnectedUtc, 
            IReferenceClock referenceClock)
        {
            Id = Guid.NewGuid();
            _tcpClient = tcpClient;
            _referenceClock = referenceClock;
            IncId = Interlocked.Increment(ref _incCounter);
            InstantConnecting = instantConnectingUtc;
            InstantConnected = instantConnectedUtc;
            LocalPort = ((IPEndPoint) tcpClient.Client.LocalEndPoint).Port;
            RemotePort = ((IPEndPoint) tcpClient.Client.RemoteEndPoint).Port;
            LocalAddress = ((IPEndPoint) tcpClient.Client.LocalEndPoint).Address;
            RemoteAddress = ((IPEndPoint) tcpClient.Client.RemoteEndPoint).Address;

            var networkStream = tcpClient.GetStream();
            
            _preferedFileName = (((IPEndPoint)tcpClient.Client.LocalEndPoint).Address) + $"-{IncId}";

            WriteStream = networkStream;
            ReadStream = networkStream;
        }

        public void Dispose()
        {
            try
            {
                WriteStream?.Dispose();
            }
            catch
            {

            }

            try
            {
                ReadStream?.Dispose();
            }
            catch
            {

            }

            try
            {
                _tcpClient?.Dispose();
            }
            catch
            {

            }


            _tcpClient = null;
            WriteStream = ReadStream = null;
        }

        public Guid Id { get; }

        public int IncId { get; }

        public Stream WriteStream { get; private set; }

        public Stream ReadStream { get; private set; }

        //public Stream Stream { get; private set;  }

        public int LocalPort { get; }

        public int RemotePort { get; }

        public IPAddress LocalAddress { get; }

        public IPAddress RemoteAddress { get; }

        public void Acquire()
        {

        }

        public Task Release(bool closeConnection)
        {
            if (closeConnection)
            {
                Dispose();
            }

            return Task.FromResult(true);
        }

        public DateTime InstantConnecting { get; }

        public DateTime InstantConnected { get; }

        public IHttpStreamReader GetHttpStreamReader()
        {
            return _httpStreamReader ?? (_httpStreamReader = new Http11StreamReader(ReadStream, _referenceClock)); 
        }

        public bool IsWebSocketConnection { get; set; }

        public void UpgradeReadStream(Stream stream, string hostName, int port)
        {
            ReadStream = stream;
            _httpStreamReader = new Http11StreamReader(ReadStream, _referenceClock);

            if (hostName != null)
                TargetHostName = hostName;

            if (port > 0)
                TargetPort = port;
        }

        public void Upgrade(Stream stream, string hostName, int port)
        {
            WriteStream = stream; // new SpyiedStream(stream, _preferedFileName);
            ReadStream = stream; // new SpyiedStream(stream, _preferedFileName);

            _httpStreamReader = new Http11StreamReader(ReadStream, _referenceClock);

            if (hostName != null)
                TargetHostName = hostName;

            if (port > 0)
                TargetPort = port;
        }

        public string TargetHostName { get; private set; }

        public int TargetPort { get; private set; }

        public bool IsSecure { get; set; }
    }
}