// Copyright © 2022 Haga Rakotoharivelo

namespace Echoes.Rules.Filters
{
    public enum FilterScope
    {
        OnAuthorityReceived,
        RequestHeaderReceivedFromClient,
        RequestBodyReceivedFromClient,
        ResponseHeaderReceivedFromRemote,
        ResponseBodyReceivedFromRemote
    }
}