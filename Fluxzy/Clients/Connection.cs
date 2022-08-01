using System;
using System.IO;
using System.Net;
using System.Threading;

namespace Fluxzy.Clients
{
    /// <summary>
    /// Contains information about transport layer 
    /// </summary>
    public class Connection : IRemoteLink
    {
        private static int _connectionIdCounter = 0;
        private int _requestProcessed;

        public Connection(Authority authority)
        {
            Authority = authority;
            Id = Interlocked.Increment(ref _connectionIdCounter);
        }

        public Stream WriteStream { get; set; }

        public Stream ReadStream { get; set; }

        public string HttpVersion { get; set; }

        public int Id { get; set; }

        public int RequestProcessed
        {
            get => _requestProcessed;
        }

        public void AddNewRequestProcessed()
        {
            Interlocked.Increment(ref _requestProcessed);
        }

        public Authority Authority { get; set; }

        public IPAddress RemoteAddress { get; set; }

        public SslInfo SslInfo { get; set; }

        public DateTime DnsSolveStart { get; set; }

        public DateTime DnsSolveEnd { get; set; }

        public DateTime TcpConnectionOpening { get; set; }

        public DateTime TcpConnectionOpened { get; set; }

        public DateTime SslNegotiationStart { get; set; }

        public DateTime SslNegotiationEnd { get; set; }

        public int LocalPort { get; set; }

        public string LocalAddress { get; set; }
    }
}