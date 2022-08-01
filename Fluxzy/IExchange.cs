// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;

namespace Fluxzy
{
    public interface IAuthority
    {
        string HostName { get; }

        int Port { get; }

        bool Secure { get; }
    }

    public interface IExchange
    {
        string FullUrl { get; }

        string KnownAuthority { get; }

        string Method { get; }

        string Path { get; }

        IEnumerable<HeaderFieldInfo> GetRequestHeaders();

        IEnumerable<HeaderFieldInfo> GetResponseHeaders();

        int StatusCode { get; }

        string EgressIp { get;  }
    }
}