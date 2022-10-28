// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Rules.Filters
{
    public enum FilterScope
    {
        OnAuthorityReceived,
        RequestHeaderReceivedFromClient,
        RequestBodyReceivedFromClient,
        ResponseHeaderReceivedFromRemote,
        ResponseBodyReceivedFromRemote,

        OutOfScope = 99999
    }
}