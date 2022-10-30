// Copyright © 2022 Haga RAKOTOHARIVELO

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text.Json.Serialization;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Fluxzy
{
    public class FluxzySetting
    {
        /// <summary>
        ///     Proxy listen address
        /// </summary>
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
        public int ConnectionPerHost { get; internal set; } = 8;

        /// <summary>
        ///     Ssl protocols for remote host connection
        /// </summary>
        public SslProtocols ServerProtocols { get; internal set; } =
            SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;

        /// <summary>
        ///     The CA certificate
        /// </summary>
        public Certificate CaCertificate { get; internal set; } = Certificate.UseDefault();

        /// <summary>
        ///     The default certificate cache directory used by echoes proxy
        /// </summary>

        public string CertificateCacheDirectory { get; internal set; } = "%appdata%/.echoes/cert-caches";

        /// <summary>
        ///     When set to true, echoes will automaticall install default certificate when starting.
        /// </summary>
        public bool AutoInstallCertificate { get; internal set; } = true;

        /// <summary>
        ///     Check whether server certificate is valid. Default value is true
        /// </summary>
        public bool CheckCertificateRevocation { get; internal set; } = true;

        /// <summary>
        ///     Do not use certificate cache. Regen certificate whenever asked
        /// </summary>
        public bool DisableCertificateCache { get; internal set; }

        public IReadOnlyCollection<string> ByPassHost { get; internal set; } =
            new List<string> { "localhost", "127.0.0.1" };

        /// <summary>
        ///     Max header length processed by the proxy
        /// </summary>
        public int MaxHeaderLength { get; set; } = 16384;

        /// <summary>
        ///     Archiving policy
        /// </summary>
        public ArchivingPolicy ArchivingPolicy { get; internal set; } = ArchivingPolicy.None;

        /// <summary>
        ///     Global alteration rules
        /// </summary>
        public List<Rule> AlterationRules { get; set; } = new();

        [JsonConstructor]
        public FluxzySetting()
        {
        }

        /// <summary>
        ///     Set hosts that bypass the proxy
        /// </summary>
        /// <param name="hosts"></param>
        /// <returns></returns>
        public FluxzySetting SetByPassedHosts(params string[] hosts)
        {
            ByPassHost = new ReadOnlyCollection<string>(hosts.Where(h => !string.IsNullOrWhiteSpace(h)).ToList());

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

        /// <summary>
        ///     Add hosts that echoes should not decrypt
        /// </summary>
        /// <param name="hosts"></param>
        /// <returns></returns>
        public FluxzySetting AddTunneledHosts(params string[] hosts)
        {
            foreach (var host in hosts.Where(h => !string.IsNullOrWhiteSpace(h)))
                AlterationRules.Add(new Rule(
                    new SkipSslTunnelingAction(),
                    new HostFilter(host, StringSelectorOperation.Exact)));

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

        public FluxzySetting SetConnectionPerHost(int connectionPerHost)
        {
            if (connectionPerHost < 1 || connectionPerHost >= 64)
                throw new ArgumentOutOfRangeException(nameof(connectionPerHost), "value should be between 1 and 64");

            ConnectionPerHost = connectionPerHost;

            return this;
        }

        public FluxzySetting SetClientCertificateOnHost(string host, Certificate certificate)
        {
            AlterationRules.Add(new Rule(new SetClientCertificateAction(certificate), new HostFilter(host)));

            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="subDomain"></param>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public FluxzySetting SetClientCertificateOnSubdomain(string subDomain, Certificate certificate)
        {
            AlterationRules.Add(new Rule(new SetClientCertificateAction(certificate),
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

        public FluxzySetting SetAutoInstallCertificate(bool value)
        {
            AutoInstallCertificate = value;

            return this;
        }

        public FluxzySetting SetSkipGlobalSslDecryption(bool value)
        {
            if (value)
                AlterationRules.Add(new Rule(new SkipSslTunnelingAction(), new AnyFilter()));

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
        ///     Get the default setting
        /// </summary>
        /// <returns></returns>
        public static FluxzySetting CreateDefault()
        {
            return new FluxzySetting
            {
                ConnectionPerHost = 8
            }.SetBoundAddress("127.0.0.1", 44344);
        }
    }
}
