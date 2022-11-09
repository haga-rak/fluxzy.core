﻿using System.Collections.Generic;
using System.Net.Security;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class ForceHttp2Action : Action
    {
        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public override string DefaultDescription => $"Force using HTTP/2.0";
        
        public override ValueTask Alter(ExchangeContext context, Exchange? exchange, Connection? connection)
        {
            // TODO avoid allocating new list here 

            context.SslApplicationProtocols = new List<SslApplicationProtocol>()
            {
                SslApplicationProtocol.Http2
            };

            return default; 
        }
    }
}