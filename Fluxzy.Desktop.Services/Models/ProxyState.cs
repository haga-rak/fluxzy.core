// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services.Models
{
    public class ProxyState
    {
        public bool IsListening { get; set; }

        public List<ProxyEndPoint> BoundConnections { get; set; }
    }
}