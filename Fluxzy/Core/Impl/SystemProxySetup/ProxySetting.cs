using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Core.SystemProxySetup
{
    internal class ProxySetting
    {
        public ProxySetting(string boundHost, int listenPort, bool enabled, params string [] byPassHosts)
        {
            BoundHost = boundHost;
            ListenPort = listenPort;
            Enabled = enabled;
            ByPassHosts = byPassHosts.ToList();
        }

        public bool Enabled { get; set; }

        public string BoundHost { get; set; } 

        public int ListenPort { get; set; } 

        public List<string> ByPassHosts { get; set; }
    }
}