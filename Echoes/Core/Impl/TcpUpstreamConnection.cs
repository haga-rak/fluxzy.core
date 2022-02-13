using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.Core
{
    internal class TcpUpstreamConnection : IUpstreamConnection
    {
        private TcpClient _tcpClient;
        private readonly IServerChannelPoolManager _originPool;
        private readonly IReferenceClock _referenceClock;
        private static int _incCounter = 0;

        internal TcpUpstreamConnection(TcpClient tcpClient,
            string hostname, IServerChannelPoolManager originPool,
            DateTime instantConnectingUtc, DateTime instantConnectedUtc,
            DateTime instantDnsSolveStartUtc, DateTime instantDnsSolveEndUtc, bool secure, 
            IReferenceClock referenceClock,
            Stream secureStream = null)
        {
            Id = Guid.NewGuid();
            IncId = Interlocked.Increment(ref _incCounter);
            Hostname = hostname;
            _tcpClient = tcpClient;
            _originPool = originPool;
            _referenceClock = referenceClock;
            InstantConnecting = instantConnectingUtc;
            InstantConnected = instantConnectedUtc;
            InstantDnsSolveStartUtc = instantDnsSolveStartUtc;
            InstantDnsSolveEndUtc = instantDnsSolveEndUtc;
            Secure = secure;

            LocalPort = ((IPEndPoint) tcpClient.Client.LocalEndPoint).Port;
            RemotePort = ((IPEndPoint) tcpClient.Client.RemoteEndPoint).Port;
            LocalAddress = ((IPEndPoint) tcpClient.Client.LocalEndPoint).Address;
            RemoteAddress = ((IPEndPoint) tcpClient.Client.RemoteEndPoint).Address;

            var originalStream = secureStream ?? tcpClient.GetStream();
            WriteStream = originalStream;
            ReadStream = originalStream;
        }

        public void Dispose()
        {
            try
            {
                WriteStream?.Dispose();
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
        }

        public Guid Id { get; }

        public int IncId { get; }

        public string Hostname { get; }

        public bool Secure { get; }

        public Stream WriteStream { get; private set; }

        public Stream ReadStream { get; private set; }


        //public Stream Stream { get; }

        public int LocalPort { get; }

        public int RemotePort { get; }

        public IPAddress LocalAddress { get; }

        public IPAddress RemoteAddress { get; }

        public void Acquire()
        {

        }

        public async Task Release(bool closeConnection)
        {
            if (_originPool != null)
                await _originPool.Return(this, closeConnection).ConfigureAwait(false);

            if (closeConnection)
            {
                Dispose();
            }
        }

        public DateTime InstantDnsSolveStartUtc { get; }


        public DateTime InstantDnsSolveEndUtc { get; }

        public bool ShouldBeClose { get; set; }


        public DateTime InstantConnecting { get; }

        public DateTime InstantConnected { get; }


        private IHttpStreamReader _httpStreamReader;

        public IHttpStreamReader GetHttpStreamReader()
        {
            return _httpStreamReader ?? (_httpStreamReader = new Http11StreamReader(ReadStream, _referenceClock));
        }

        public bool IsWebSocketConnection { get; set; }

        public override bool Equals(object obj)
        {
            return obj is TcpUpstreamConnection connection &&
                   Id.Equals(connection.Id);
        }

        public override int GetHashCode()
        {
            return 2108858624 + EqualityComparer<Guid>.Default.GetHashCode(Id);
        }


        public DateTime ExpireInstant { get;  set; }
    }
}