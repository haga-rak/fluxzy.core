// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Fluxzy.Clients.Headers;
using Fluxzy.Clients.Mock;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Rules;

namespace Fluxzy.Clients
{
    public class ExchangeContext
    {
        public ExchangeContext(IAuthority authority, 
            VariableContext variableContext, FluxzySetting? fluxzySetting)
        {
            Authority = authority;
            VariableContext = variableContext;
            FluxzySetting = fluxzySetting;
        }

        public IAuthority Authority { get; set; }

        /// <summary>
        ///     Host IP that shall be used instead of a classic DNS resolution
        /// </summary>
        public IPAddress? RemoteHostIp { get; set; }

        /// <summary>
        ///     Port of substitution
        /// </summary>
        public int? RemoteHostPort { get; set; }

        /// <summary>
        ///     Client certificate for this exchange
        /// </summary>
        public X509Certificate2Collection? ClientCertificates { get; set; }

        /// <summary>
        ///     true if fluxzy should not decrypt this exchange
        /// </summary>
        public bool BlindMode { get; set; }

        public bool ForceNewConnection { get; set; } = false;

        public PreMadeResponse? PreMadeResponse { get; set; }

        public List<SslApplicationProtocol>? SslApplicationProtocols { get; set; }

        public SslProtocols ProxyTlsProtocols { get; set; } = SslProtocols.None;

        public bool SkipRemoteCertificateValidation { get; set; } = false;

        public List<HeaderAlteration> RequestHeaderAlterations { get; } = new();

        public List<HeaderAlteration> ResponseHeaderAlterations { get; } = new();

        public BreakPointContext? BreakPointContext { get; set; }

        public VariableContext VariableContext { get; }

        public VariableBuildingContext? VariableBuildingContext { get; set; } = null;

        public FluxzySetting? FluxzySetting { get;  }

        public IPAddress DownStreamLocalAddressStruct { get; set; }

        public int ProxyListenPort { get; set; }
    }
}
