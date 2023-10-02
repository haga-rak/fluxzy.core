// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MessagePack;

namespace Fluxzy
{
    [MessagePackObject]
    public class DownstreamErrorInfo
    {
        private static int _errorId = 0;

        [Key(0)]
        public int ErrorId { get; private set; }

        [Key(1)]
        public DateTime InstantDateUtc { get; private set; }

        [Key(2)]
        public string SourceIp { get; private set; }

        [Key(3)]
        public int SourcePort { get; private set; }

        [Key(4)]
        public string Message { get; private set; }

        [Key(5)]
        public string LongDescription { get; private set; }

        [Key(6)]
        public string ? RequiredHost { get; private set; }

        public static DownstreamErrorInfo CreateFrom(TcpClient client, Exception ex)
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
