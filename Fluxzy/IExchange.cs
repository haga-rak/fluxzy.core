// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using Fluxzy.Clients.H11;

namespace Fluxzy
{
    public interface IExchange
    {
        int Id { get; }

        string FullUrl { get; }

        string KnownAuthority { get; }

        int KnownPort { get; }

        string HttpVersion { get; }

        string Method { get; }

        string Path { get; }

        int StatusCode { get; }

        string? EgressIp { get; }

        string? Comment { get; }

        HashSet<Tag>? Tags { get; }

        bool IsWebSocket { get; }

        List<WsMessage>? WebSocketMessages { get; }

        Agent? Agent { get; }

        List<ClientError> ClientErrors { get; }

        IEnumerable<HeaderFieldInfo> GetRequestHeaders();

        IEnumerable<HeaderFieldInfo>? GetResponseHeaders();
    }
}
