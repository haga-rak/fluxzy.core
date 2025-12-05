// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text.Json.Serialization;
using Fluxzy.Certificates;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Actions.HighLevelActions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Fluxzy
{
    /// <summary>
    ///     The main configuration for starting a proxy instance
    /// </summary>
    public partial class FluxzySetting
    {
        [JsonConstructor]
        public FluxzySetting()
        {
        }

        [JsonInclude]
        [Obsolete("Used only for serialization")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public List<Rule> InternalAlterationRules { get; internal set; } = new();

        /// <summary>
        ///     Proxy listen address
        /// </summary>
        [JsonInclude]
        public HashSet<ProxyBindPoint> BoundPoints { get; internal set; } = new();

        /// <summary>
        ///     Returns a friendly description of the bound points
        /// </summary>
        public string BoundPointsDescription =>
            string.Join(", ", BoundPoints
                              .OrderByDescending(d => d.Default)
                              .Select(s => $"[{s.EndPoint}]"));

        /// <summary>
        ///     Verbose mode
        /// </summary>
        public bool Verbose { get; internal set; }

        /// <summary>
        ///     Number of concurrent connection per host maintained by the connection pool excluding websocket connections. Default
        ///     value is 16.
        /// </summary>
        [JsonInclude]
        public int ConnectionPerHost { get; internal set; } = FluxzySharedSetting.MaxConnectionPerHost;

        /// <summary>
        ///     Ssl protocols for remote host connection
        /// </summary>
        [JsonInclude]
        public SslProtocols ServerProtocols { get; internal set; } =
#pragma warning disable SYSLIB0039
            SslProtocols.Tls
            | SslProtocols.Tls11
            | SslProtocols.Tls12
#if NETCOREAPP3_1_OR_GREATER
            | SslProtocols.Tls13
#endif
            ;
#pragma warning restore SYSLIB0039

        /// <summary>
        ///     The CA certificate used for decryption
        /// </summary>
        [JsonInclude]
        public Certificate CaCertificate { get; internal set; } = Certificate.UseDefault();

        /// <summary>
        ///     The default certificate cache directory. Setting this value helps improving performance because producing
        ///     root certificate on the fly is expensive.
        /// </summary>
        [JsonInclude]
        public string CertificateCacheDirectory { get; internal set; } = "%appdata%/.fluxzy/cert-caches";

        /// <summary>
        ///     When set to true, fluxzy will automatically install default certificate when starting.
        /// </summary>
        [JsonInclude]
        public bool AutoInstallCertificate { get; internal set; }

        /// <summary>
        ///     Check whether server certificate is valid. Default value is true
        /// </summary>
        [JsonInclude]
        public bool CheckCertificateRevocation { get; internal set; } = true;

        /// <summary>
        ///     Do not use certificate cache. Regenerate certificate whenever asked
        /// </summary>
        [JsonInclude]
        public bool DisableCertificateCache { get; internal set; }

        /// <summary>
        ///     True if fluxzy should capture raw packet matching exchanges
        /// </summary>
        [JsonInclude]
        public bool CaptureRawPacket { get; internal set; }

        /// <summary>
        /// </summary>
        [JsonInclude]
        public string? CaptureInterfaceName { get; internal set; }

        /// <summary>
        ///     Hosts that by pass proxy
        /// </summary>
        public IReadOnlyCollection<string> ByPassHost =>
            ByPassHostFlat.Split(new[] { ";", ",", "\r", "\n", "\t" }, StringSplitOptions.RemoveEmptyEntries)
                          .Distinct().ToList();

        [JsonInclude]
        public string ByPassHostFlat { get; internal set; } = "";

        /// <summary>
        ///     Archiving policy
        /// </summary>
        [JsonInclude]
        public ArchivingPolicy ArchivingPolicy { get; internal set; } = ArchivingPolicy.None;

        /// <summary>
        ///     Global alteration rules
        /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete for JSON serialization
        public ReadOnlyCollection<Rule> AlterationRules => new(InternalAlterationRules);
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>
        ///     Specify a filter which trigger save to directory when passed.
        ///     When this filter is null, all exchanges will be saved.
        /// </summary>
        [JsonInclude]
        public Filter? SaveFilter { get; internal set; }

        /// <summary>
        ///     Skip SSL decryption for any exchanges. This setting cannot be overriden by rules
        /// </summary>
        [JsonInclude]
        public bool GlobalSkipSslDecryption { get; internal set; }

        /// <summary>
        ///     When set to true, the raw network capture will be done out of process.
        /// </summary>
        [JsonInclude]
        public bool OutOfProcCapture { get; internal set; } = true;

        /// <summary>
        ///     Using bouncy castle for ssl streams instead of OsDefault (SChannel or OpenSSL)
        /// </summary>
        [JsonInclude]
        public bool UseBouncyCastle { get; internal set; }

        /// <summary>
        ///     Fluxzy will exit when the number of exchanges reaches this value.
        ///     Default value is null (no limit)
        /// </summary>
        [JsonInclude]
        public int? MaxExchangeCount { get; set; }

        /// <summary>
        ///     No proxy CONNECT parsing. The client will send directly requests as if fluxzy is a web server.
        /// </summary>
        [JsonInclude]
        public bool ReverseMode { get; internal set; }

        /// <summary>
        ///     When reverse mode forced port is set, fluxzy will use the specified port to connect to the remote instead of client
        ///     connected port
        /// </summary>
        [JsonInclude]
        public int? ReverseModeForcedPort { get; internal set; }

        /// <summary>
        ///     When set to true, fluxzy will expect plain http for reverse mode
        /// </summary>
        [JsonInclude]
        public bool ReverseModePlainHttp { get; internal set; }

        /// <summary>
        ///     The current proxy authentication setting if any
        /// </summary>
        [JsonInclude]
        public ProxyAuthentication? ProxyAuthentication { get; internal set; }

        /// <summary>
        ///     Use a provided configuration file instead of default
        ///     to determine user agent used in UserAgentAction
        /// </summary>
        [JsonInclude]
        public string? UserAgentActionConfigurationFile { get; internal set; }

        /// <summary>
        ///     Skip remote certificate validation, default false
        /// </summary>
        [JsonInclude]
        public bool SkipRemoteCertificateValidation { get; internal set; }

        /// <summary>
        ///    Indicates whether Fluxzy should use HTTP/2 for client connections when supported.
        /// </summary>
        [JsonInclude]
        public bool ServeH2 { get; internal set; }

        internal IEnumerable<Rule> FixedRules()
        {
            if (GlobalSkipSslDecryption) {
                yield return new Rule(new SkipSslTunnelingAction(), AnyFilter.Default);
            }

            yield return new Rule(
                new MountCertificateAuthorityAction(), new FilterCollection(new IsSelfFilter(),
                    new PathFilter("/ca", StringSelectorOperation.StartsWith)) {
                    Operation = SelectorCollectionOperation.And
                });

            yield return new Rule(
                new MountWelcomePageAction(), new IsSelfFilter());
        }

        /// <summary>
        ///     Create a default setting for a fluxzy capture session.
        ///     Fluxzy will listen on 127.0.0.1 on port 44344
        /// </summary>
        /// <returns></returns>
        public static FluxzySetting CreateDefault()
        {
            return new FluxzySetting {
                ConnectionPerHost = FluxzySharedSetting.MaxConnectionPerHost
            }.SetBoundAddress(IPAddress.Loopback, 44344);
        }

        /// <summary>
        ///     Create a default setting for a fluxzy capture session with provided address and port
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static FluxzySetting CreateDefault(IPAddress address, int port)
        {
            return new FluxzySetting {
                ConnectionPerHost = FluxzySharedSetting.MaxConnectionPerHost
            }.SetBoundAddress(address, port);
        }

        /// <summary>
        ///     Create a default instance and listen on IPv4 loopback address
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static FluxzySetting CreateLocal(int port = 44344)
        {
            return CreateDefault(IPAddress.Loopback, port);
        }

        /// <summary>
        ///     Creates a FluxzySetting with a randomly generated local port number and bound to the IPv4 loopback address.
        /// </summary>
        /// <returns>A FluxzySetting object with the local port number set to a random value.</returns>
        public static FluxzySetting CreateLocalRandomPort()
        {
            return CreateLocal(0);
        }
    }
}
