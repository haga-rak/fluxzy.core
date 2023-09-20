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
    public class FluxzySetting
    {
        [JsonInclude()]
        [Obsolete("Used only for serialization")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public List<Rule> InternalAlterationRules = new();

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
        ///     value is 8.
        /// </summary>
        [JsonInclude]
        public int ConnectionPerHost { get; internal set; } = 16;

        /// <summary>
        ///     Ssl protocols for remote host connection
        /// </summary>
        [JsonInclude]
        public SslProtocols ServerProtocols { get; internal set; } =
#pragma warning disable SYSLIB0039
            SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
#pragma warning restore SYSLIB0039

        /// <summary>
        ///     The CA certificate used for decryption
        /// </summary>
        public Certificate CaCertificate { get; set; } = Certificate.UseDefault();

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
        public bool CaptureRawPacket { get; set; }

        /// <summary>
        /// </summary>
        [JsonInclude]
        public string? CaptureInterfaceName { get; internal set; }

        /// <summary>
        ///     Hosts that by pass proxy
        /// </summary>
        [JsonInclude]
        public List<string> ByPassHost
        {
            get
            {
                return ByPassHostFlat.Split(new[] { ";", ",", "\r", "\n", "\t" }, StringSplitOptions.RemoveEmptyEntries)
                                     .Distinct().ToList();
            }
        }

        public string ByPassHostFlat { get; set; } = "";

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
        public Filter? SaveFilter { get; set; }

        /// <summary>
        ///     Skip SSL decryption for any exchanges. This setting cannot be overriden by rules
        /// </summary>
        public bool GlobalSkipSslDecryption { get; set; } = false;

        /// <summary>
        ///     When set to true, the raw network capture will be done out of process.
        /// </summary>
        public bool OutOfProcCapture { get; set; } = true;

        /// <summary>
        ///     Using bouncy castle for ssl streams instead of OsDefault (SChannel or OpenSSL)
        /// </summary>
        public bool UseBouncyCastle { get; set; } = false;

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
                new MountWelcomePageAction
                {
                    InternalScope = FilterScope.DnsSolveDone
                }, new IsSelfFilter());
        }

        /// <summary>
        ///     Set hosts that bypass the proxy
        /// </summary>
        /// <param name="hosts"></param>
        /// <returns></returns>
        public FluxzySetting SetByPassedHosts(params string[] hosts)
        {
            ByPassHostFlat = string.Join(";", hosts.Distinct());
            return this;
        }

        /// <summary>
        ///     Set archiving policy
        /// </summary>
        /// <param name="archivingPolicy"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public FluxzySetting SetArchivingPolicy(ArchivingPolicy archivingPolicy)
        {
            ArchivingPolicy = archivingPolicy ?? throw new ArgumentNullException(nameof(archivingPolicy));

            return this;
        }

        public FluxzySetting SetOutDirectory(string directoryName)
        {
            ArchivingPolicy = Fluxzy.ArchivingPolicy.CreateFromDirectory(directoryName);
            return this;
        }

        /// <summary>
        ///     Add hosts that fluxzy should not decrypt
        /// </summary>
        /// <param name="hosts"></param>
        /// <returns></returns>
        public FluxzySetting AddTunneledHosts(params string[] hosts)
        {
            foreach (var host in hosts.Where(h => !string.IsNullOrWhiteSpace(h)))
            {
                InternalAlterationRules.Add(new Rule(
                    new SkipSslTunnelingAction(),
                    new HostFilter(host, StringSelectorOperation.Exact)));
            }

            return this;
        }

        public FluxzySetting ClearBoundAddresses()
        {
            BoundPoints.Clear();

            return this;
        }

        public FluxzySetting AddBoundAddress(IPEndPoint endpoint, bool? @default = null)
        {
            var isDefault = @default ?? BoundPoints.All(e => !e.Default);
            BoundPoints.Add(new ProxyBindPoint(endpoint, isDefault));

            return this;
        }

        public FluxzySetting AddBoundAddress(string boundAddress, int port, bool? @default = null)
        {
            if (!IPAddress.TryParse(boundAddress, out var address))
                throw new ArgumentException($"{boundAddress} is not a valid IP address");

            if (port < 0 || port >= ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port), $"port should be between 1 and {ushort.MaxValue}");

            return AddBoundAddress(new IPEndPoint(address, port), @default);
        }

        public FluxzySetting AddBoundAddress(IPAddress boundAddress, int port, bool? @default = null)
        {
            if (port < 0 || port >= ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port), $"port should be between 1 and {ushort.MaxValue}");

            return AddBoundAddress(new IPEndPoint(boundAddress, port), @default);
        }

        public FluxzySetting SetBoundAddress(string boundAddress, int port)
        {
            if (!IPAddress.TryParse(boundAddress, out var address))
                throw new ArgumentException($"{boundAddress} is not a valid IP address");

            if (port < 0 || port >= ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port), $"port should be between 1 and {ushort.MaxValue}");

            BoundPoints.Clear();
            BoundPoints.Add(new ProxyBindPoint(new IPEndPoint(address, port), true));

            return this;
        }

        public FluxzySetting SetBoundAddress(IPAddress boundAddress, int port)
        {
            if (port < 0 || port >= ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port), $"port should be between 1 and {ushort.MaxValue}");

            BoundPoints.Clear();
            BoundPoints.Add(new ProxyBindPoint(new IPEndPoint(boundAddress, port), true));

            return this;
        }

        public FluxzySetting SetConnectionPerHost(int connectionPerHost)
        {
            if (connectionPerHost < 1 || connectionPerHost >= 64)
                throw new ArgumentOutOfRangeException(nameof(connectionPerHost), "value should be between 1 and 64");

            ConnectionPerHost = connectionPerHost;

            return this;
        }

        public FluxzySetting SetClientCertificateOnHost(string host, Certificate certificate)
        {
            InternalAlterationRules.Add(new Rule(new SetClientCertificateAction(certificate), new HostFilter(host)));

            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="subDomain"></param>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public FluxzySetting SetClientCertificateOnSubdomain(string subDomain, Certificate certificate)
        {
            InternalAlterationRules.Add(new Rule(new SetClientCertificateAction(certificate),
                new HostFilter(subDomain, StringSelectorOperation.EndsWith)));

            return this;
        }

        public FluxzySetting SetServerProtocols(SslProtocols protocols)
        {
            ServerProtocols = protocols;

            return this;
        }

        public FluxzySetting SetCheckCertificateRevocation(bool value)
        {
            CheckCertificateRevocation = value;

            return this;
        }

        /// <summary>
        /// If true, fluxzy will automatically install the certificate in the user store.
        /// This call needs administrator/root privileges. 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public FluxzySetting SetAutoInstallCertificate(bool value)
        {
            AutoInstallCertificate = value;

            return this;
        }

        public FluxzySetting SetSkipGlobalSslDecryption(bool value)
        {
            if (value)
                InternalAlterationRules.Add(new Rule(new SkipSslTunnelingAction(), new AnyFilter()));

            return this;
        }

        /// <summary>
        ///     Change the default certificate used by fluxzy
        /// </summary>
        /// <returns></returns>
        public FluxzySetting SetCaCertificate(Certificate caCertificate)
        {
            CaCertificate = caCertificate;

            return this;
        }

        public FluxzySetting SetDisableCertificateCache(bool value)
        {
            DisableCertificateCache = value;

            return this;
        }

        /// <summary>
        /// Remove existing alteration rules
        /// </summary>
        /// <returns></returns>
        public FluxzySetting ClearAlterationRules()
        {
            InternalAlterationRules.Clear();
            return this;
        }

        /// <summary>
        /// Add alteration rules
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        public FluxzySetting AddAlterationRules(params Rule[] rules)
        {
            InternalAlterationRules.AddRange(rules);
            return this;
        }

        /// <summary>
        /// Add alteration rules
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        public FluxzySetting AddAlterationRules(IEnumerable<Rule> rules)
        {
            InternalAlterationRules.AddRange(rules);
            return this;
        }

        /// <summary>
        /// Add alteration rules from a config file
        /// </summary>
        /// <param name="plainConfiguration"></param>
        /// <returns></returns>
        public FluxzySetting AddAlterationRules(string plainConfiguration)
        {
            RuleConfigParser parser = new RuleConfigParser();

            var ruleSet = parser.TryGetRuleSetFromYaml(plainConfiguration, out var readErrors);

            if (readErrors != null && readErrors.Any())
                throw new ArgumentException($"Invalid configuration:\r\n {string.Join("\r\n", readErrors)}");

            AddAlterationRules(ruleSet!.Rules.SelectMany(s => s.GetAllRules()));
            return this;
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