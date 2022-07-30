// Copyright © 2022 Haga Rakotoharivelo

namespace Echoes.Rules.Filters;

public enum FilterScope
{
    RequestHeaderReceivedFromClient = 1,
    RequestBodyReceivedFromClient,
    ResponseHeaderReceivedFromRemote,
    ResponseBodyReceivedFromRemote
}