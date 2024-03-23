// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///    Instruct fluxzy to use an upstream proxy.
    /// </summary>
    [ActionMetadata("Use an upstream proxy.", NonDesktopAction = true)]
    public class UpStreamProxyAction : Action
    {
        public UpStreamProxyAction(string host, int port)
        {
            Host = host;
            Port = port;
        }

        [ActionDistinctive]
        public string Host { get; set; }

        [ActionDistinctive]
        public int Port { get; set; }

        [ActionDistinctive]
        public string? ProxyAuthorizationHeader { get; set; }

        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public override string DefaultDescription => $"Upstream proxy to {Host}:{Port}"; 

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.ProxyConfiguration = new ProxyConfiguration(Host, Port, ProxyAuthorizationHeader);
            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample("Use an upstream proxy to 192.168.1.9 on port 8080",
                new UpStreamProxyAction("192.168.1.9", 8080));

            yield return new ActionExample("Use an upstream proxy to 192.168.1.9 on port 8080 with basic auth" +
                                           " login: leeloo , password: multipass",
                new UpStreamProxyAction("192.168.1.9", 8080) {
                    ProxyAuthorizationHeader = "Basic bGVlbG9vOm11bHRpcGFzcw=="
                });
        }
    }
}
