namespace Fluxzy.Desktop.Services
{
    public class SystemProxyState
    {
        public SystemProxyState(string address, int port, bool on)
        {
            Address = address;
            Port = port;
            On = on;
        }

        public bool On { get; set;  }

        public string Address { get; set; } 

        public int Port { get; set; }
    }
}