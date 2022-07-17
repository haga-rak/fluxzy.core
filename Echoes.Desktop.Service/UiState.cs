// Copyright © 2022 Haga Rakotoharivelo

namespace Echoes.Desktop.Service
{
    public class UiState
    {
        public IFileState ?  FileStateState { get; set; }

        public ProxyState  ProxyState { get; set; }

        public EchoesSettings Settings { get; set; }
    }

    public class ProxyState
    {
        public bool IsSystemProxy { get; set; }

        public bool IsListening { get; set; }

        public List<ProxyEndPoint> BoundConnections { get; set; }
    }

    public class ProxyEndPoint
    {
        public string Address { get; set; } 

        public int Port { get; set; }
    }


    public class EchoesSettings
    {
        public ProxyStartupSetting StartupSetting { get; set; }
    }
}