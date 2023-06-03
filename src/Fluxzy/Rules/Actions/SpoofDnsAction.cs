// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Fix statically the remote ip, remote port used for the targeted exchange.
    /// </summary>
    [ActionMetadata(
        "Fix statically the remote ip or port disregards to the dns or host resolution of the current running system. " +
        "Use this action to force the resolution of a hostname to a fixed IP address. ")]
    public class SpoofDnsAction : Action
    {
        /// <summary>
        ///     The IP address, leave blank to reuse the DNS solved IP
        /// </summary>
        [ActionDistinctive]
        public string? RemoteHostIp { get; set; }

        /// <summary>
        ///     Leave blank to use the same port as specified originally by downstream
        /// </summary>
        [ActionDistinctive]
        public int? RemoteHostPort { get; set; }

        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public override string DefaultDescription =>
            RemoteHostIp == null ? "Spoof dns".Trim() : $"Spoof dns {RemoteHostIp}:{RemoteHostPort}".Trim();

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            var remoteHostIp = RemoteHostIp.EvaluateVariable(context);

            if (!string.IsNullOrEmpty(remoteHostIp)) {
                if (!IPAddress.TryParse(remoteHostIp, out var ip))
                    throw new RuleExecutionFailureException($"{remoteHostIp} is not a valid IP address");

                context.RemoteHostIp = ip;
            }

            if (RemoteHostPort != null) {
                if (RemoteHostPort < 0 || RemoteHostPort > 65535)
                    throw new RuleExecutionFailureException(
                        $"{RemoteHostPort} is not a valid port. Port must be between 0 and 65536 exclusive.");

                context.RemoteHostPort = RemoteHostPort;
            }

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample("Force the remote IP and port to be respectively 127.0.0.1 and 8080", new
                SpoofDnsAction {
                    RemoteHostIp = "127.0.0.1",
                    RemoteHostPort = 8080
                });

            yield return new ActionExample(
                "Force the remote IP to be 127.0.0.1 (port remains the same as request by the client)", new
                    SpoofDnsAction {
                        RemoteHostIp = "127.0.0.1"
                    });

            yield return new ActionExample(
                "Force the remote port to be 8080 (IP remains the same as request by the client)", new
                    SpoofDnsAction {
                        RemoteHostIp = "127.0.0.1"
                    });
        }
    }
}
