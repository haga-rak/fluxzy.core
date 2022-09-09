// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services.Models
{
    public class ProxyState
    {
        public List<ProxyEndPoint> BoundConnections { get; set; } = new(); 

        public bool OnError { get; set; }

        public string?  Message { get; set; }
    }
}