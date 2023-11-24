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

        /// <summary>
        ///  An unique identifier for this connection relative to the current fluxzy session
        /// </summary>
        public int Id { get; internal set; }

        /// <summary>
        ///  The HTTP version used for this connection
        /// </summary>
        public string? HttpVersion { get; internal set; }

        /// <summary>
        ///  The number of request processed by this connection
        /// </summary>
        public int RequestProcessed => _requestProcessed;

        /// <summary>
        /// The authority used for this connection
        /// </summary>
        public Authority Authority { get; internal set; }

        /// <summary>
        ///  The remote address of the connection
        /// </summary>
        public IPAddress? RemoteAddress { get; internal set; }

        /// <summary>
        ///  The remote port of the connection
        /// </summary>
        public int LocalPort { get; internal set; }

        /// <summary>
        /// SSL information if any
        /// </summary>
        public SslInfo? SslInfo { get; internal set; }
        
        public DateTime DnsSolveStart { get; internal set; }

        public DateTime DnsSolveEnd { get; internal set; }

        public DateTime TcpConnectionOpening { get; internal set; }

        public DateTime TcpConnectionOpened { get; internal set; }

        public DateTime SslNegotiationStart { get; internal set; }

        public DateTime SslNegotiationEnd { get; internal set; }

        /// <summary>
        ///  Address of the local endpoint
        /// </summary>
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
