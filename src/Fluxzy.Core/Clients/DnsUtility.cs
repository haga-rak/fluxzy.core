// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Threading.Tasks;
using Fluxzy.Clients.Mock;
using Fluxzy.Core;
using Fluxzy.Rules;

namespace Fluxzy.Clients
{
    internal readonly struct DnsResolutionResult
    {
        public DnsResolutionResult(IPEndPoint endPoint, DateTime dnsSolveStart, DateTime dnsSolveEnd)
        {
            EndPoint = endPoint;
            DnsSolveStart = dnsSolveStart;
            DnsSolveEnd = dnsSolveEnd;
        }

        public IPEndPoint EndPoint { get;  }

        public DateTime DnsSolveStart { get; }

        public DateTime DnsSolveEnd { get; }
    }

    internal static class DnsUtility
    {
        public static async ValueTask<(DnsResolutionResult, MockedConnectionPool ?)>
            ComputeDnsUpdateExchange(Exchange exchange, 
                ITimingProvider timingProvider, IDnsSolver dnsSolver, 
                ProxyRuntimeSetting? runtimeSetting)
        {
            var dnsSolveStart = timingProvider.Instant();
            var connectHostName = exchange.Context.ProxyConfiguration?.Host ?? exchange.Authority.HostName;


            var ipAddress = exchange.Context.RemoteHostIp ??
                            await dnsSolver.SolveDns(connectHostName).ConfigureAwait(false);

            var dnsSolveEnd = timingProvider.Instant();

            var remotePort = exchange.Context.ProxyConfiguration?.Port ?? exchange.Context.RemoteHostPort 
                             ?? exchange.Authority.Port;

            exchange.Context.RemoteHostIp = ipAddress;
            exchange.Context.RemoteHostPort = remotePort;

            var remoteEndPoint = new IPEndPoint(ipAddress, remotePort);

            if (runtimeSetting != null) {

                await runtimeSetting.EnforceRules(exchange.Context,
                    FilterScope.DnsSolveDone,
                    exchange.Connection, exchange).ConfigureAwait(false);

                if (exchange.Context.PreMadeResponse != null)
                    return (new(remoteEndPoint, dnsSolveStart, dnsSolveEnd), new MockedConnectionPool(
                        exchange.Authority, exchange.Context.PreMadeResponse));
            }

            return (new(remoteEndPoint, dnsSolveStart, dnsSolveEnd), null);
        }

        public static async ValueTask<DnsResolutionResult>
            ComputeDns(Authority authority, ITimingProvider timingProvider, IDnsSolver dnsSolver)
        {
            var dnsSolveStart = timingProvider.Instant();
            var ipAddress = await dnsSolver.SolveDns(authority.HostName).ConfigureAwait(false);
            var dnsSolveEnd = timingProvider.Instant();
            var remotePort = authority.Port;
            var remoteEndPoint = new IPEndPoint(ipAddress, remotePort);
            return new(remoteEndPoint, dnsSolveStart, dnsSolveEnd);
        }

    }
}
