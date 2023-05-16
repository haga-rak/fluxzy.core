// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Text.Json.Serialization;
using Fluxzy.Clients;
using MessagePack;

namespace Fluxzy
{
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

        [Key(0)]
        public int Id { get; private set; }

        [Key(1)]
        public string? HttpVersion { get; private set; }

        [Key(2)]
        public AuthorityInfo Authority { get; private set; }

        [Key(3)]
        public SslInfo? SslInfo { get; private set; }

        [Key(4)]
        public int RequestProcessed { get; set; }

        [Key(5)]
        public DateTime DnsSolveStart { get; private set; }

        [Key(6)]
        public DateTime DnsSolveEnd { get; private set; }

        [Key(7)]
        public DateTime TcpConnectionOpening { get; private set; }

        [Key(8)]
        public DateTime TcpConnectionOpened { get; private set; }

        [Key(9)]
        public DateTime SslNegotiationStart { get; private set; }

        [Key(10)]
        public DateTime SslNegotiationEnd { get; private set; }

        [Key(11)]
        public int LocalPort { get; private set; }

        [Key(12)]
        public string? LocalAddress { get; private set; }

        [Key(13)]
        public string? RemoteAddress { get; private set; }
    }
}
