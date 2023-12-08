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
    /// <summary>
    /// Holds the mutable state of the ongoing exchange
    /// </summary>
    public class ExchangeContext
    {
        public ExchangeContext(
            IAuthority authority,
            VariableContext variableContext, FluxzySetting? fluxzySetting)
        {
            Authority = authority;
            VariableContext = variableContext;
            FluxzySetting = fluxzySetting;
        }

        /// <summary>
        /// Remote authority, this value is used to build the host header on H11/request and
        /// :authority pseudo header on H2/request
        /// </summary>
        public IAuthority Authority { get; set; }
        
        /// <summary>
        ///  If the ongoing connection to Authority should use TLS
        /// </summary>
        public bool Secure { get; set; }

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
        /// </summary>
        public X509Certificate2? ServerCertificate { get; set; }

        /// <summary>
        ///     true if fluxzy should not decrypt this exchange
        /// </summary>
        public bool BlindMode { get; set; }

        /// <summary>
        ///     If true, fluxzy will not try to reuse an existing connection
        /// </summary>
        public bool ForceNewConnection { get; set; } = false;

        /// <summary>
        ///     Abort  the current exchange
        /// </summary>
        public bool Abort { get; set; } = false;

        /// <summary>
        ///     Provide a premade response (a mock)
        /// </summary>
        public PreMadeResponse? PreMadeResponse { get; set; }

        /// <summary>
        ///     Available ALPN protocols, leave null to use default
        /// </summary>
        public List<SslApplicationProtocol>? SslApplicationProtocols { get; set; }

        /// <summary>
        ///     Available TLS protocols, leave null to use default
        /// </summary>
        public SslProtocols ProxyTlsProtocols { get; set; } = SslProtocols.None;

        /// <summary>
        ///     Don't validate the remote certificate
        /// </summary>
        public bool SkipRemoteCertificateValidation { get; set; } = false;

        /// <summary>
        /// Gets or sets the list of header alterations for the request.
        /// </summary>
        /// <value>
        /// The list of <see cref="HeaderAlteration"/> objects representing the header alterations for the request.
        /// </value>
        public List<HeaderAlteration> RequestHeaderAlterations { get; } = new();

        /// <summary>
        /// Gets or sets the list of response header alterations.
        /// </summary>
        /// <value>
        /// The list of response header alterations.
        /// </value>
        public List<HeaderAlteration> ResponseHeaderAlterations { get; } = new();

        /// <summary>
        /// Holds information about a breakpoint context.
        /// </summary>
        public BreakPointContext? BreakPointContext { get; set; }

        /// <summary>
        /// Gets the variable context associated with the current object.
        /// </summary>
        /// <remarks>
        /// The VariableContext property provides access to the variable context, which represents the scope and lifetime of variables used within the current object. The variable context stores
        /// variables as key-value pairs and allows access to their values.
        /// </remarks>
        /// <returns>
        /// The variable context associated with the current object.
        /// </returns>
        public VariableContext VariableContext { get; }

        /// <summary>
        ///   Information about the ongoing exchange and connection
        /// </summary>
        public VariableBuildingContext? VariableBuildingContext { get; internal set; } = null;

        /// <summary>
        ///  The proxy setting
        /// </summary>
        public FluxzySetting? FluxzySetting { get; }

        /// <summary>
        /// Gets or sets the down stream local IP address of the struct.
        /// </summary>
        /// <value>
        /// The down stream local IP address.
        /// </value>
        public IPAddress DownStreamLocalAddressStruct { get; set; } = null!;

        /// <summary>
        ///  Information about the proxy port that has been used to retrieve the ongoing exchange
        /// </summary>
        public int ProxyListenPort { get; internal set; }

        internal NetworkStream? UnderlyingBcStream { get; set; }

        internal DisposeEventNotifierStream? EventNotifierStream { get; set; }

        internal Dictionary<Filter, bool> FilterEvaluationResult { get; } = new();

        /// <summary>
        /// Gets or sets the request body substitution.
        /// </summary>
        public IStreamSubstitution? RequestBodySubstitution { get; set; }

        /// <summary>
        /// Gets or sets the substitution for the response body.
        /// </summary>
        public IStreamSubstitution? ResponseBodySubstitution { get; set; }
    }
}
