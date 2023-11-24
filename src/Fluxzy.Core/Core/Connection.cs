// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net;
using System.Threading;
using Fluxzy.Clients;

namespace Fluxzy.Core
{
    /// <summary>
    ///     Contains information about transport layer
    /// </summary>
    public class Connection : IRemoteLink
    {
        private int _requestProcessed;

        public Connection(Authority authority, IIdProvider idProvider)
        {
            Authority = authority;
            Id = idProvider.NextConnectionId();
        }

        public int Id { get; internal set; }

        public string? HttpVersion { get; internal set; }

        public int RequestProcessed => _requestProcessed;

        public Authority Authority { get; internal set; }

        public IPAddress? RemoteAddress { get; internal set; }

        public SslInfo? SslInfo { get; internal set; }

        public DateTime DnsSolveStart { get; internal set; }

        public DateTime DnsSolveEnd { get; internal set; }

        public DateTime TcpConnectionOpening { get; internal set; }

        public DateTime TcpConnectionOpened { get; internal set; }

        public DateTime SslNegotiationStart { get; internal set; }

        public DateTime SslNegotiationEnd { get; internal set; }
        
        public int LocalPort { get; internal set; }

        public string? LocalAddress { get; internal set; }

        public Stream? WriteStream { get; internal set; }

        public Stream? ReadStream { get; internal set; }

        public int TimeoutIdleSeconds { get; internal set; } = -1; 

        public void AddNewRequestProcessed()
        {
            Interlocked.Increment(ref _requestProcessed);
        }
    }
}
