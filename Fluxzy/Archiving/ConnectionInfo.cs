using System;
using System.Text.Json.Serialization;
using Fluxzy.Clients;

namespace Fluxzy
{
    public class ConnectionInfo
    {
        [JsonConstructor]
        public ConnectionInfo()
        {
        }

        public ConnectionInfo(Connection original)
        {
            Id = original.Id;
            DnsSolveStart = original.DnsSolveStart;
            DnsSolveEnd = original.DnsSolveEnd;
            TcpConnectionOpening = original.TcpConnectionOpening;
            TcpConnectionOpened = original.TcpConnectionOpened;
            SslNegotiationStart = original.SslNegotiationStart;
            SslNegotiationEnd = original.SslNegotiationEnd;
            RequestProcessed = original.RequestProcessed;
            LocalPort = original.LocalPort;
            LocalAddress = original.LocalAddress;
            RemoteAddress = original.RemoteAddress.ToString();
            Authority = new AuthorityInfo(original.Authority);
            SslInfo = original.SslInfo;
        }

        public int Id { get; set; }

        public AuthorityInfo Authority { get; set; }

        public SslInfo SslInfo { get; set; }

        public int RequestProcessed { get; set; }

        public DateTime DnsSolveStart { get; set; }

        public DateTime DnsSolveEnd { get; set; }

        public DateTime TcpConnectionOpening { get; set; }

        public DateTime TcpConnectionOpened { get; set; }

        public DateTime SslNegotiationStart { get; set; }

        public DateTime SslNegotiationEnd { get; set; }

        public int LocalPort { get; set; }

        public string LocalAddress { get; set; }

        public string RemoteAddress { get; set; }
    }
}