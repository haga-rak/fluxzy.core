using System;
using System.Threading;

namespace Echoes
{
    /// <summary>
    /// Contains information about transport layer 
    /// </summary>
    public class Connection
    {
        private static int _connectionIdCounter = 0; 
        
        public Connection(Authority authority)
        {
            Authority = authority;
            Id = Interlocked.Increment(ref _connectionIdCounter);
        }

        public string HttpVersion { get; set; }

        public int Id { get; set; }

        public Authority Authority { get; set; }

        public DateTime TcpConnectionOpening { get; set; }

        public DateTime TcpConnectionOpened { get; set; }

        public DateTime SslNegotiationStart { get; set; }

        public DateTime SslNegotiationEnd { get; set; }
    }
}