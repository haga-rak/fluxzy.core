using System;
using Echoes.Clients;

namespace Echoes
{
    public readonly struct ConnectionInfo
    {
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
            Authority = new AuthorityInfo(original.Authority);
            SslInfo = original.SslInfo; 
        }

        public int Id { get;  }

        public AuthorityInfo Authority { get; }

        public SslInfo SslInfo { get; }

        public int RequestProcessed { get;  }

        public DateTime DnsSolveStart { get;  }

        public DateTime DnsSolveEnd { get;  }

        public DateTime TcpConnectionOpening { get;  }

        public DateTime TcpConnectionOpened { get;  }

        public DateTime SslNegotiationStart { get;  }

        public DateTime SslNegotiationEnd { get;  }
    }

    public readonly struct AuthorityInfo
    {
        public AuthorityInfo(Authority original)
        {
            HostName = original.HostName;
            Port = original.Port;
            Secure = original.Secure;
        }

        public string HostName { get; }

        public int Port { get; }

        public bool Secure { get;  }
    }
}