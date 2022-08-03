// Copyright © 2022 Haga Rakotoharivelo

using System.IO;

namespace Fluxzy.Clients.Mock
{
    public abstract class PremadeResponse
    {
        public abstract string GetFlatH11Header(Authority authority); 

        public abstract Stream ReadBody(Authority authority); 
    }
}