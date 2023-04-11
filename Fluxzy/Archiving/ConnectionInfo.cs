// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Text.Json.Serialization;
using Fluxzy.Clients;

namespace Fluxzy
{
    public class ConnectionInfo
    {
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

        public int Id { get; }

        public string? HttpVersion { get; }

        public AuthorityInfo Authority { get; }

        public SslInfo? SslInfo { get; }

        public int RequestProcessed { get; set; }

        public DateTime DnsSolveStart { get; }

        public DateTime DnsSolveEnd { get; }

        public DateTime TcpConnectionOpening { get; }

        public DateTime TcpConnectionOpened { get; }

        public DateTime SslNegotiationStart { get; }

        public DateTime SslNegotiationEnd { get; }

        public int LocalPort { get; }

        public string? LocalAddress { get; }

        public string? RemoteAddress { get; }
    }
}
