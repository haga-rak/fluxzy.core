using System;
using System.IO;
using System.Threading;

namespace Echoes
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

        public DateTime TcpConnectionOpening { get; set; }

        public DateTime TcpConnectionOpened { get; set; }

        public DateTime SslNegotiationStart { get; set; }

        public DateTime SslNegotiationEnd { get; set; }
    }
}