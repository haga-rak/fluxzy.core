// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Encoding;

namespace Echoes.H2
{
    /// <summary>
    /// Represents a connection pool to the same end point.
    /// </summary>
    public interface IHttpConnectionPool : IAsyncDisposable, IDisposable
    {
        Authority Authority { get; }
        
        Task Init();
        
        ValueTask Send(Exchange exchange, CancellationToken cancellationToken = default);
    }


    public class PendingRequest
    {
        public PendingRequest(ICollection<HeaderField> requestHeaders)
        {
            RequestHeaders = requestHeaders;
        }

        public ICollection<HeaderField> RequestHeaders { get; }
    }
    

    public readonly struct Authority : IEquatable<Authority>
    {
        public Authority(string hostName, int port, bool secure)
        {
            HostName = hostName;
            Port = port;
            Secure = secure;
        }

        public string HostName { get;  }

        public int Port { get;  }

        public bool Equals(Authority other)
        {
            return
                string.Equals(HostName, other.HostName, StringComparison.OrdinalIgnoreCase)
                && Port == other.Port && Secure == other.Secure;
        }

        public override bool Equals(object obj)
        {
            return obj is Authority other && Equals(other);
        }

        public override int GetHashCode()
        {
            Span<char> destBuffer = stackalloc char[HostName.Length];
            return HashCode.Combine(HostName.AsSpan().ToLowerInvariant(destBuffer), Port, Secure);
        }

        public bool Secure { get;  }
    }
}