// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MessagePack;

namespace Fluxzy
{
    /// <summary>
    /// Holds information about a downstream error (error between Fluxzy and the client)
    /// </summary>
    [MessagePackObject]
    public class DownstreamErrorInfo
    {
        private static int _errorId = 0;

        /// <summary>
        /// A unique error id relative to the current capture session
        /// </summary>
        [Key(0)]
        public int ErrorId { get; private set; }

        /// <summary>
        /// Instant date of the error
        /// </summary>
        [Key(1)]
        public DateTime InstantDateUtc { get; private set; }

        /// <summary>
        /// Source IP address
        /// </summary>
        [Key(2)]
        public string SourceIp { get; private set; }

        /// <summary>
        /// Source port
        /// </summary>
        [Key(3)]
        public int SourcePort { get; private set; }

        /// <summary>
        /// Error message
        /// </summary>
        [Key(4)]
        public string Message { get; private set; }

        /// <summary>
        /// Error message Description
        /// </summary>
        [Key(5)]
        public string LongDescription { get; private set; }

        /// <summary>
        /// The host that caused the error if any
        /// </summary>
        [Key(6)]
        public string ? RequiredHost { get; private set; }

        internal static DownstreamErrorInfo CreateFrom(TcpClient client, Exception ex)
        {
            var endPoint = (IPEndPoint) client.Client.RemoteEndPoint!;

            return new DownstreamErrorInfo {
                ErrorId = Interlocked.Increment(ref _errorId),
                Message = ex.Message,
                InstantDateUtc = DateTime.UtcNow,
                SourceIp = endPoint.Address.ToString(),
                SourcePort = endPoint.Port,
                RequiredHost = ex is FluxzyException fluxzyException ? fluxzyException.TargetHost : null,
            }; 
        }
    }
}
