// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    [ActionMetadata("Forward request to a specific URL. This make fluxzy act as a reverse proxy.")]
    public class ForwardAction : Action
    {
        public ForwardAction(string url)
        {
            Url = url;

            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
                throw new InvalidOperationException("Provided URL is not a valid one"); 
        }

        public string Url { get;  }


        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => $"Forward request to {Url}".Trim();

        public override ValueTask Alter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (exchange == null)
                return default;

            if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri))
                throw new InvalidOperationException("Provided URL is not a valid one. Must be an absolute URI.");

            var originalPath = exchange.Request.Header.Path.ToString();


            if (Uri.TryCreate(originalPath, 
                    UriKind.Absolute, out var path)) {
                originalPath = path.PathAndQuery;
            }

            var finalPath = string.Empty; 

            if (uri.PathAndQuery == "/") {
                finalPath = originalPath;
            }
            else
            {
                finalPath =
                    uri!.PathAndQuery + originalPath;
            }

            var hostName = uri.Host;
            var port = uri.Port;
            var scheme = uri.Scheme;

            exchange.Request.Header.Path = finalPath.AsMemory();
            exchange.Request.Header.Authority = $"{hostName}:{port}".AsMemory(); 
            exchange.Request.Header.Scheme = scheme.AsMemory();

            exchange.Authority = new Authority(hostName, port, string.Equals(scheme, "https",
                StringComparison.OrdinalIgnoreCase));

            if (connection != null)
                connection.Authority = exchange.Authority;

            context.Authority = exchange.Authority;

            return default;
        }
    }
}
