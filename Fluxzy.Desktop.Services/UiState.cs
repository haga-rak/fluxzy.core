// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services
{
    public class UiState
    {
        public FileState ?  FileStateState { get; set; }

        public ProxyState  ProxyState { get; set; }

        public FluxzySettingsHolder SettingsHolder { get; set; }
    }

    public class ProxyState
    {
        public bool IsSystemProxyOn { get; set; }

        public bool IsListening { get; set; }

        public List<ProxyEndPoint> BoundConnections { get; set; }
    }

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