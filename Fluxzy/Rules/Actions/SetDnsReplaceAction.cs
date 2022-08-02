// Copyright © 2022 Haga Rakotoharivelo

using System.Net;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class SetDnsReplaceAction : IAction
    {
        public IPAddress RemoteHostIp { get; set; }

        public int RemoteHostPort { get; set; }

        public FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public Task Alter(ExchangeContext context, Exchange exchange, Connection connection)
        {
            context.RemoteHostIp = RemoteHostIp;
            context.RemoteHostPort = RemoteHostPort;

            return Task.CompletedTask;
        }
    }
}