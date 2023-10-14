// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Text.Json.Serialization;
using Fluxzy.Core;
using MessagePack;

namespace Fluxzy
{
    /// <summary>
    /// Holds information about a connection between fluxzy and a remote server
    /// </summary>
    [MessagePackObject]
    public class ConnectionInfo
    {
        [SerializationConstructor]
#pragma warning disable CS8618
        private ConnectionInfo()
#pragma warning restore CS8618
        {

        }

        public ConnectionInfo(Connection original)
        {
            Id = original.Id;
            HttpVersion = original.HttpVersion;
            DnsSolveStart = original.DnsSolveStart;
            DnsSolveEnd = original.DnsSolveEnd;
            TcpConnectionOpening = original.TcpConnectionOpening;
            TcpConnectionOpened = original.TcpConnectionOpened;
            SslNegotiationStart = original.SslNegotiationStart;
            SslNegotiationEnd = original.SslNegotiationEnd;
            RequestProcessed = original.RequestProcessed;
            LocalPort = original.LocalPort;
            LocalAddress = original.LocalAddress;
            RemoteAddress = original.RemoteAddress?.ToString();
            Authority = new AuthorityInfo(original.Authority);
            SslInfo = original.SslInfo;
        }

        [JsonConstructor]
        public ConnectionInfo(
            int id, AuthorityInfo authority, SslInfo? sslInfo, int requestProcessed, DateTime dnsSolveStart,
            DateTime dnsSolveEnd, DateTime tcpConnectionOpening, DateTime tcpConnectionOpened,
            DateTime sslNegotiationStart, DateTime sslNegotiationEnd, int localPort, string localAddress,
            string remoteAddress, string? httpVersion)
        {
            Id = id;
            Authority = authority;
            SslInfo = sslInfo;
            RequestProcessed = requestProcessed;
            DnsSolveStart = dnsSolveStart;
            DnsSolveEnd = dnsSolveEnd;
            TcpConnectionOpening = tcpConnectionOpening;
            TcpConnectionOpened = tcpConnectionOpened;
            SslNegotiationStart = sslNegotiationStart;
            SslNegotiationEnd = sslNegotiationEnd;
            LocalPort = localPort;
            LocalAddress = localAddress;
            RemoteAddress = remoteAddress;
            HttpVersion = httpVersion;
        }
        
        /// <summary>
        /// The connection id 
        /// </summary>
        [Key(0)]
        public int Id { get; private set; }

        /// <summary>
        /// The HTTP version used on this connection can be HTTP/1.1 or HTTP/2
        /// </summary>
        [Key(1)]
        public string? HttpVersion { get; private set; }

        /// <summary>
        /// The remote authority of this exchange
        /// </summary>
        [Key(2)]
        public AuthorityInfo Authority { get; private set; }

        /// <summary>
        /// The SSL information of this connection
        /// </summary>
        [Key(3)]
        public SslInfo? SslInfo { get; private set; }

        /// <summary>
        /// The number of exchange processed by this exchange
        /// </summary>
        [Key(4)]
        public int RequestProcessed { get; set; }

        /// <summary>
        /// Instant the DNS solve started
        /// </summary>
        [Key(5)]
        public DateTime DnsSolveStart { get; private set; }

        /// <summary>
        /// Instant the DNS solve ended
        /// </summary>
        [Key(6)]
        public DateTime DnsSolveEnd { get; private set; }

        /// <summary>
        /// Instant the TCP connection started
        /// </summary>
        [Key(7)]
        public DateTime TcpConnectionOpening { get; private set; }

        /// <summary>
        /// Instant the TCP connection opened
        /// </summary>
        [Key(8)]
        public DateTime TcpConnectionOpened { get; private set; }

        /// <summary>
        /// Instant the SSL negotiation started
        /// </summary>
        [Key(9)]
        public DateTime SslNegotiationStart { get; private set; }

        /// <summary>
        /// Instant the SSL negotiation end
        /// </summary>
        [Key(10)]
        public DateTime SslNegotiationEnd { get; private set; }

        /// <summary>
        /// Client port used on fluxzy side
        /// </summary>
        [Key(11)]
        public int LocalPort { get; private set; }

        /// <summary>
        /// Client address used on fluxzy side
        /// </summary>
        [Key(12)]
        public string? LocalAddress { get; private set; }

        /// <summary>
        /// The remote address used 
        /// </summary>
        [Key(13)]
        public string? RemoteAddress { get; private set; }
    }
}
