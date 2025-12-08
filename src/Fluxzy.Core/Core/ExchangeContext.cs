// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Clients;
using Fluxzy.Clients.Headers;
using Fluxzy.Clients.Mock;
using Fluxzy.Clients.Ssl;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Extensions;
using Fluxzy.Misc.Streams;
using Fluxzy.Rules;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Core
{
    /// <summary>
    ///     Holds the mutable state of the ongoing exchange
    /// </summary>
    public class ExchangeContext
    {
        private List<IStreamSubstitution>? _requestBodyStreamSubstitutions;

        private List<IStreamSubstitution>? _responseBodySubstitutions;

        public ExchangeContext(
            IAuthority authority,
            VariableContext variableContext, FluxzySetting? fluxzySetting,
            SetUserAgentActionMapping setUserAgentActionMapping)
        {
            Authority = authority;
            VariableContext = variableContext;
            FluxzySetting = fluxzySetting;
            SetUserAgentActionMapping = setUserAgentActionMapping;
            SkipRemoteCertificateValidation = fluxzySetting?.SkipRemoteCertificateValidation ?? false;
            AdvancedTlsSettings.ExportCertificateInSslInfo = fluxzySetting?.ExportCertificateInSslInfo ?? false;
        }

        /// <summary>
        ///     Remote authority, this value is used to build the host header on H11/request and
        ///     :authority pseudo header on H2/request
        /// </summary>
        public IAuthority Authority { get; set; }

        /// <summary>
        ///     If the ongoing connection to Authority should use TLS
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
        public List<Certificate>? ClientCertificates { get; set; }

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
        ///     Gets or sets advanced TLS settings
        /// </summary>
        public AdvancedTlsSettings AdvancedTlsSettings { get; set; } = new();

        /// <summary>
        ///     Don't validate the remote certificate
        /// </summary>
        public bool SkipRemoteCertificateValidation { get; set; }

        /// <summary>
        ///     Gets or sets the list of header alterations for the request.
        /// </summary>
        /// <value>
        ///     The list of <see cref="HeaderAlteration" /> objects representing the header alterations for the request.
        /// </value>
        public List<HeaderAlteration> RequestHeaderAlterations { get; } = new();

        /// <summary>
        ///     Gets or sets the list of response header alterations.
        /// </summary>
        /// <value>
        ///     The list of response header alterations.
        /// </value>
        public List<HeaderAlteration> ResponseHeaderAlterations { get; } = new();

        /// <summary>
        ///     Holds information about a breakpoint context.
        /// </summary>
        public BreakPointContext? BreakPointContext { get; set; }

        /// <summary>
        ///     Gets the variable context associated with the current object.
        /// </summary>
        /// <remarks>
        ///     The VariableContext property provides access to the variable context, which represents the scope and lifetime of
        ///     variables used within the current object. The variable context stores
        ///     variables as key-value pairs and allows access to their values.
        /// </remarks>
        /// <returns>
        ///     The variable context associated with the current object.
        /// </returns>
        public VariableContext VariableContext { get; }

        /// <summary>
        ///     Information about the ongoing exchange and connection
        /// </summary>
        public VariableBuildingContext? VariableBuildingContext { get; internal set; } = null;

        /// <summary>
        ///     The proxy setting
        /// </summary>
        public FluxzySetting? FluxzySetting { get; }

        public SetUserAgentActionMapping SetUserAgentActionMapping { get; }

        /// <summary>
        ///     Gets or sets the down stream local IP address of the struct.
        /// </summary>
        /// <value>
        ///     The down stream local IP address.
        /// </value>
        public IPAddress DownStreamLocalAddressStruct { get; set; } = null!;

        /// <summary>
        ///     Gets or sets the dns over https name or url.
        /// </summary>
        public string? DnsOverHttpsNameOrUrl { get; set; } = null;

        /// <summary>
        ///     Information about the proxy port that has been used to retrieve the ongoing exchange
        /// </summary>
        public int ProxyListenPort { get; internal set; }

        /// <summary>
        ///     Upstream proxy configuration
        /// </summary>
        public ProxyConfiguration? ProxyConfiguration { get; set; }

        internal NetworkStream? UnderlyingBcStream { get; set; }

        internal DisposeEventNotifierStream? EventNotifierStream { get; set; }

        internal Dictionary<Filter, bool> FilterEvaluationResult { get; } = new();

        /// <summary>
        ///     True if the current exchange has at least one response body substitution
        /// </summary>
        public bool HasResponseBodySubstitution => _responseBodySubstitutions != null;

        /// <summary>
        ///     True if the current exchange has at least one request body substitution
        /// </summary>
        public bool HasRequestBodySubstitution => _requestBodyStreamSubstitutions != null;

        /// <summary>
        ///     Define if the exchange has a request body
        /// </summary>
        public bool HasRequestBody { get; set; }

        /// <summary>
        /// </summary>
        public bool DnsOverHttpsCapture { get; set; }

        public bool AlwaysSendClientCertificate { get; set; }

        /// <summary>
        ///     Register a response body substitution
        /// </summary>
        /// <param name="substitution"></param>
        public void RegisterResponseBodySubstitution(IStreamSubstitution substitution)
        {
            if (_responseBodySubstitutions == null) {
                _responseBodySubstitutions = new List<IStreamSubstitution>();
            }

            _responseBodySubstitutions.Add(substitution);
        }

        /// <summary>
        ///     Register a request body substitution
        /// </summary>
        /// <param name="substitution"></param>
        public void RegisterRequestBodySubstitution(IStreamSubstitution substitution)
        {
            if (_requestBodyStreamSubstitutions == null) {
                _requestBodyStreamSubstitutions = new List<IStreamSubstitution>();
            }

            _requestBodyStreamSubstitutions.Add(substitution);
        }

        internal ValueTask<Stream> GetSubstitutedResponseBody(
            Stream original, bool chunkedTransfer, CompressionType compressionType)
        {
            // remove compression from exchange 

            var decoded = original;

            if (chunkedTransfer) {
                decoded = CompressionHelper.GetUnChunkedStream(decoded);
            }

            if (compressionType != CompressionType.None) {
                decoded = CompressionHelper.GetDecodedStream(compressionType, decoded);
            }

            return SubstitutionHelper.GetSubstitutedStream(decoded, _responseBodySubstitutions ??
                                                                    throw new InvalidOperationException());
        }

        internal ValueTask<Stream> GetSubstitutedRequestBody(Stream original, Exchange exchange)
        {
            var decoded = exchange.GetDecodedRequestBodyStream(original, out var compressionType);

            if (compressionType != CompressionType.None) {
                exchange.Request.Header.RemoveHeader("content-encoding");
            }

            exchange.Request.Header.RemoveHeader("transfer-encoding");

            return SubstitutionHelper.GetSubstitutedStream(decoded, _requestBodyStreamSubstitutions ??
                                                                    throw new InvalidOperationException());
        }
    }

    internal static class SubstitutionHelper
    {
        public static async ValueTask<Stream> GetSubstitutedStream(
            Stream originalStream, IEnumerable<IStreamSubstitution> substitutions)
        {
            var finalStream = originalStream;

            foreach (var substitution in substitutions) {
                finalStream = await substitution.Substitute(finalStream).ConfigureAwait(false);
            }

            return finalStream;
        }
    }
}
