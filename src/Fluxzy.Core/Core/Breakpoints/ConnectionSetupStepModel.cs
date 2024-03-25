// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using Fluxzy.Clients.Dns;

namespace Fluxzy.Core.Breakpoints
{
    public class ConnectionSetupStepModel : IBreakPointAlterationModel
    {
        /// <summary>
        ///     Force exchange to create new connection instead picking from connection pool.
        ///     If this setting is false, the following properties may not being set as the connection is picked
        ///     from the connection pool.
        /// </summary>
        public bool ForceNewConnection { get; set; } = true; 

        /// <summary>
        ///     Whether we should skip certificate validation
        /// </summary>

        public bool SkipRemoteCertificateValidation { get; set; }

        /// <summary>
        ///     Use Ip Address
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        ///     Used port
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        ///     Initialization of the alteration model
        /// </summary>
        /// <param name="exchange"></param>
        public ValueTask Init(Exchange exchange)
        {
            // We do nothing to init 
            
            IpAddress = exchange.EgressIp ?? exchange.Context.RemoteHostIp?.ToString() ?? "-/-";
            Port = exchange.Context.RemoteHostPort ?? exchange.Authority.Port;
            
            ForceNewConnection = true;
            SkipRemoteCertificateValidation = exchange.Context.SkipRemoteCertificateValidation;

            return default;
        }

        public async ValueTask Alter(Exchange exchange)
        {
            if (!string.IsNullOrWhiteSpace(IpAddress) && IPAddress.TryParse(IpAddress, out var ip))
                exchange.Context.RemoteHostIp = ip;
            else {
                exchange.Context.RemoteHostIp =
                    (await new DefaultDnsSolver().SolveDnsQuietly(exchange.Authority.HostName).ConfigureAwait(false));

                if (exchange.Context.RemoteHostIp != null)
                    IpAddress = exchange.Context.RemoteHostIp.ToString();
            }

            if (Port != null && Port > 0 && Port < ushort.MaxValue)
                exchange.Context.RemoteHostPort = Port.Value;

            exchange.Context.ForceNewConnection = ForceNewConnection;
            exchange.Context.SkipRemoteCertificateValidation = SkipRemoteCertificateValidation;

            Done = true;
        }

        public bool Done { get; private set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }
}
