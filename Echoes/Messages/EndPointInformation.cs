using Echoes.Core;
using Newtonsoft.Json;

namespace Echoes
{
    public class EndPointInformation
    {
        public EndPointInformation(IConnection connection)
        {
            LocalAddress = connection?.LocalAddress.ToString();
            LocalPort = connection?.LocalPort ?? 0;
            RemoteAddress = connection?.RemoteAddress?.ToString();
            RemotePort = connection?.RemotePort ?? 0;
        }

        internal EndPointInformation(string localAddress, int localPort, string remoteAddress, int remotePort)
        {
            LocalAddress = localAddress;
            LocalPort = localPort;
            RemoteAddress = remoteAddress;
            RemotePort = remotePort;
        }

        [JsonProperty]
        public string LocalAddress { get; set; }

        [JsonProperty]
        public int LocalPort { get; set; }

        [JsonProperty]
        public string RemoteAddress { get; set; }

        [JsonProperty]
        public int RemotePort { get; set; }
    }
}