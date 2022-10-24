// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;
using Fluxzy.Clients.H11;

namespace Fluxzy
{
    public interface IExchange
    {
        string FullUrl { get; }

        string KnownAuthority { get; }

        string HttpVersion { get;  }

        string Method { get; }

        string Path { get; }

        IEnumerable<HeaderFieldInfo> GetRequestHeaders();

        IEnumerable<HeaderFieldInfo>? GetResponseHeaders();

        int StatusCode { get; }

        string? EgressIp { get;  }

        string? Comment { get; }

        HashSet<Tag>? Tags { get;  }

        bool IsWebSocket { get; }


        List<WsMessage>? WebSocketMessages { get; }


        //AgentInfo?  AgentInfo { get; }
    }

    //public class AgentInfo
    //{
    //    public string ? OsName { get; set; }

    //    public string ? AgentName { get; set; }

    //    public string ? AgentVersion { get; set; }
    //}
}