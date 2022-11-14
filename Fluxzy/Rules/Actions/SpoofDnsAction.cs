// Copyright © 2022 Haga Rakotoharivelo

using System.Net;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    /// Fix statically the remote ip, remote port used for the targeted exchange. 
    /// </summary>
    public class SpoofDnsAction : Action
    {
        /// <summary>
        /// The IP address, leave blank to reuse the DNS solved IP
        /// </summary>
        public IPAddress?  RemoteHostIp { get; set; }

        /// <summary>
        /// Leave blank to use the same port as specified originally by downstream
        /// </summary>
        public int ? RemoteHostPort { get; set; }

        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public override ValueTask Alter(ExchangeContext context, Exchange? exchange, Connection? connection)
        {
            context.RemoteHostIp = RemoteHostIp;
            context.RemoteHostPort = RemoteHostPort;

            return default;
        }

        public override string DefaultDescription =>
            RemoteHostIp == null ? 
            $"Spoof dns".Trim(): 
            $"Spoof dns {RemoteHostIp}:{RemoteHostPort}".Trim();
    }
}