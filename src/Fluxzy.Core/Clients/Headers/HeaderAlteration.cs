// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core;

namespace Fluxzy.Clients.Headers
{
    public abstract class HeaderAlteration
    {
        public abstract void Apply(Header header);
    }
}
