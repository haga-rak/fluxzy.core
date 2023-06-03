// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using Fluxzy.Core;

namespace Fluxzy.Clients.Mock
{
    public abstract class PreMadeResponse
    {
        public abstract string GetFlatH11Header(Authority authority);

        public abstract Stream ReadBody(Authority authority);
    }
}
