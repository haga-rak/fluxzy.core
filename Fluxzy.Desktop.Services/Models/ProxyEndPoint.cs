// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services.Models
{
    public class ProxyEndPoint
    {
        public ProxyEndPoint(string address, int port)
        {
            Address = address;
            Port = port;
        }

        public string Address { get; set; }

        public int Port { get; set; }
    }
}