// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Fluxzy.Clients.Headers;
using Fluxzy.Clients.Mock;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Misc.Streams;
using Fluxzy.Rules;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Core
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
        /// 
        /// </summary>
        public X509Certificate2? ServerCertificate { get; set; }

        /// <summary>
        ///     true if fluxzy should not decrypt this exchange
        /// </summary>
        public bool BlindMode { get; set; }

        /// <summary>
        /// If true, fluxzy will not try to reuse an existing connection
        /// </summary>
        public bool ForceNewConnection { get; set; } = false;

        /// <summary>
        ///  Abort  the current exchange
        /// </summary>
        public bool Abort { get; set; } = false; 

        /// <summary>
        ///    Provide a premade response (a mock)
        /// </summary>
        public PreMadeResponse? PreMadeResponse { get; set; }

        /// <summary>
        ///   Available ALPN protocols, leave null to use default
        /// </summary>
        public List<SslApplicationProtocol>? SslApplicationProtocols { get; set; }


        /// <summary>
        ///    Available TLS protocols, leave null to use default
        /// </summary>
        public SslProtocols ProxyTlsProtocols { get; set; } = SslProtocols.None;

        /// <summary>
        ///  Don't validate the remote certificate
        /// </summary>
        public bool SkipRemoteCertificateValidation { get; set; } = false;

        public List<HeaderAlteration> RequestHeaderAlterations { get; } = new();

        public List<HeaderAlteration> ResponseHeaderAlterations { get; } = new();

        public BreakPointContext? BreakPointContext { get; set; }

        public VariableContext VariableContext { get; }

        public VariableBuildingContext? VariableBuildingContext { get; set; } = null;

        public FluxzySetting? FluxzySetting { get; }

        public IPAddress DownStreamLocalAddressStruct { get; set; }

        public int ProxyListenPort { get; set; }

        public bool Secure { get; set; }

        public NetworkStream? UnderlyingBcStream { get; set; }

        public DisposeEventNotifierStream? EventNotifierStream { get; set; }

        public Dictionary<Filter, bool> FilterEvaluationResult { get; } = new();

        public IStreamSubstitution? RequestBodySubstitution { get; set; }

        public IStreamSubstitution? ResponseBodySubstitution { get; set; }
    }
}
