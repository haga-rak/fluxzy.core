﻿// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class AddResponseHeaderAction : Action
    {
        public AddResponseHeaderAction(string headerName, string headerValue)
        {
            HeaderName = headerName;
            HeaderValue = headerValue;
        }

        public string HeaderName { get; set;  }

        public string HeaderValue { get; set;  }

        public override FilterScope ActionScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public override Task Alter(ExchangeContext context, Exchange exchange, Connection connection)
        {
            exchange.Response.Header.AltAddHeader(
                HeaderName,
                HeaderValue
            );

            return Task.CompletedTask;
        }
    }
}