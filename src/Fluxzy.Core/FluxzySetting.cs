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
    public partial class FluxzySetting
    {
        [JsonInclude()]
        [Obsolete("Used only for serialization")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public List<Rule> InternalAlterationRules { get; private set; }

        [JsonConstructor]
        public FluxzySetting()
        {
        }

        /// <summary>
        ///     Proxy listen address
        /// </summary>
        [JsonInclude]
        public HashSet<ProxyBindPoint> BoundPoints { get; internal set; } = new();

        /// <summary>
        ///     Returns a friendly description of the bound points
        /// </summary>
        public string BoundPointsDescription
        {
            get
            {
                return string.Join(", ", BoundPoints
                                         .OrderByDescending(d => d.Default)
                                         .Select(s => $"[{s.EndPoint}]"));
            }
        }

        /// <summary>
        ///     Verbose mode
        /// </summary>
        public bool Verbose { get; internal set; }

        /// <summary>
        ///     Number of concurrent connection per host maintained by the connection pool excluding websocket connections. Default
        ///     value is 16.
        /// </summary>
        [JsonInclude]
        public int ConnectionPerHost { get; internal set; } = 16;


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
        [Obsolete("This option is ignored when set directly. Use CapturedTcpConnectionProvider to enable raw capture.")]
        public bool CaptureRawPacket { get; internal set; }

        /// <summary>
        /// </summary>
        [JsonInclude]
        public string? CaptureInterfaceName { get; internal set; }

        /// <summary>
        ///     Hosts that by pass proxy
        /// </summary>
        public IReadOnlyCollection<string> ByPassHost
        {
            get
            {
                return ByPassHostFlat.Split(new[] { ";", ",", "\r", "\n", "\t" }, StringSplitOptions.RemoveEmptyEntries)
                                     .Distinct().ToList();
            }
        }

        [JsonInclude]
        public string ByPassHostFlat { get; internal set; } = "";

        /// <summary>
        ///     Archiving policy
        /// </summary>
        public ArchivingPolicy ArchivingPolicy { get; internal set; } = ArchivingPolicy.None;

        /// <summary>
        ///     Global alteration rules
        /// </summary>
        public ReadOnlyCollection<Rule> AlterationRules => new ReadOnlyCollection<Rule>(InternalAlterationRules);

        /// <summary>
        ///     Specify a filter which trigger save to directory when passed.
        ///     When this filter is null, all exchanges will be saved.
        /// </summary>
        public Filter? SaveFilter { get; internal set; }

        /// <summary>
        ///     Skip SSL decryption for any exchanges. This setting cannot be overriden by rules
        /// </summary>
        [JsonInclude]
        public bool GlobalSkipSslDecryption { get; internal set; } = false;

        /// <summary>
        ///     When set to true, the raw network capture will be done out of process.
        /// </summary>
        public bool OutOfProcCapture { get; set; } = true;

        /// <summary>
        ///     Using bouncy castle for ssl streams instead of OsDefault (SChannel or OpenSSL)
        /// </summary>
        public bool UseBouncyCastle { get; internal set; } = false;

        /// <summary>
        ///     Fluxzy will exit when the number of exchanges reaches this value.
        ///     Default value is null (no limit)
        /// </summary>
        public int? MaxExchangeCount { get; set; }


        internal IEnumerable<Rule> FixedRules()
        {
            if (GlobalSkipSslDecryption)
                yield return new Rule(new SkipSslTunnelingAction(), AnyFilter.Default);

            yield return new Rule(
                new MountCertificateAuthorityAction(), new FilterCollection(new IsSelfFilter(),
                    new PathFilter("/ca", StringSelectorOperation.StartsWith))
                {
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
            return new FluxzySetting
            {
                ConnectionPerHost = 16
            }.SetBoundAddress("127.0.0.1", 44344);
        }

        /// <summary>
        ///    Create a default setting for a fluxzy capture session with provided address and port
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static FluxzySetting CreateDefault(IPAddress address, int port)
        {
            return new FluxzySetting
            {
                ConnectionPerHost = 16
            }.SetBoundAddress(address, port);
        }

    }

    
}