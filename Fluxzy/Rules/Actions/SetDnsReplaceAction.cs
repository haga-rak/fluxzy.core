﻿// Copyright © 2022 Haga Rakotoharivelo

using System.Net;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class SetDnsReplaceAction : Action
    {
        public IPAddress RemoteHostIp { get; set; }

        public int RemoteHostPort { get; set; }

        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public override Task Alter(ExchangeContext context, Exchange exchange, Connection connection)
        {
            context.RemoteHostIp = RemoteHostIp;
            context.RemoteHostPort = RemoteHostPort;

            return Task.CompletedTask;
        }
    }
}