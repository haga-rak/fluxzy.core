// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

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
