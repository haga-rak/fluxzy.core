namespace Fluxzy.Desktop.Services
{
    public class SystemProxyState
    {
        public bool On { get; set; }

        public string Address { get; set; }

        public int Port { get; set; }

        public SystemProxyState(string address, int port, bool on)
        {
            Address = address;
            Port = port;
            On = on;
        }
    }
}
