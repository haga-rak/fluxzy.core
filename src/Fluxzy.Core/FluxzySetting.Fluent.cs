using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using Fluxzy.Certificates;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Fluxzy
{
    public partial class FluxzySetting
    {
        public FluxzySetting SetSaveFilter(Filter saveFilter)
        {
            SaveFilter = saveFilter;
            return this;
        }

        public FluxzySetting ClearSaveFilter()
        {
            SaveFilter = null;
            return this;
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

        /// <summary>
        ///  Clear all bound addresses
        /// </summary>
        /// <returns></returns>
        public FluxzySetting ClearBoundAddresses()
        {
            BoundPoints.Clear();

            return this;
        }

        /// <summary>
        ///  Append a new endpoint to the bound address list
        /// </summary>
        /// <param name="endpoint">The IP address and port to listen to</param>
        /// <param name="default">If this Endpoint is the default endpoint. When true, the automatic system proxy address will prioritize this endpoint</param>
        /// <returns></returns>
        public FluxzySetting AddBoundAddress(IPEndPoint endpoint, bool? @default = null)
        {
            var isDefault = @default ?? BoundPoints.All(e => !e.Default);
            BoundPoints.Add(new ProxyBindPoint(endpoint, isDefault));

            return this;
        }

        /// <summary>
        /// Append a new endpoint to the bound address list
        /// </summary>
        /// <param name="boundAddress">Valid IPv4 or IPv6 address to listen to</param>
        /// <param name="port">Port number to listen to</param>
        /// <param name="default">If this Endpoint is the default endpoint. When true, the automatic system proxy address will prioritize this endpoint</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public FluxzySetting AddBoundAddress(string boundAddress, int port, bool? @default = null)
        {
            if (!IPAddress.TryParse(boundAddress, out var address))
                throw new ArgumentException($"{boundAddress} is not a valid IP address");

            if (port < 0 || port >= ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port), $"port should be between 1 and {ushort.MaxValue}");

            return AddBoundAddress(new IPEndPoint(address, port), @default);
        }

        /// <summary>
        /// Append a new endpoint to the bound address list
        /// </summary>
        /// <param name="boundAddress">Valid IPv4 or IPv6 address to listen to. This property accepts IpAddress.Any (0.0.0.0).</param>
        /// <param name="port">Port number to listen to</param>
        /// <param name="default">If this Endpoint is the default endpoint. When true, the automatic system proxy address will prioritize this endpoint</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public FluxzySetting AddBoundAddress(IPAddress boundAddress, int port, bool? @default = null)
        {
            if (port < 0 || port >= ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port), $"port should be between 1 and {ushort.MaxValue}");

            return AddBoundAddress(new IPEndPoint(boundAddress, port), @default);
        }

        /// <summary>
        ///  Clear all bound addresses and set a new one
        /// </summary>
        /// <param name="boundAddress"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
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

        /// <summary>
        /// Clear all bound addresses and set a new one
        /// </summary>
        /// <param name="boundAddress">Valid IPv4 or IPv6 address to listen to. This property accepts IpAddress.Any (0.0.0.0).</param>
        /// <param name="port">Port number to listen to</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public FluxzySetting SetBoundAddress(IPAddress boundAddress, int port)
        {
            if (port < 0 || port >= ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port), $"port should be between 1 and {ushort.MaxValue}");

            BoundPoints.Clear();
            BoundPoints.Add(new ProxyBindPoint(new IPEndPoint(boundAddress, port), true));

            return this;
        }

        /// <summary>
        ///     Set the number of concurrent connection per host maintained by the connection pool excluding websocket connections.
        ///     This option is ignored for H2 remote connection 
        /// </summary>
        /// <param name="connectionPerHost"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>

        public FluxzySetting SetConnectionPerHost(int connectionPerHost)
        {
            if (connectionPerHost < 1 || connectionPerHost >= 64)
                throw new ArgumentOutOfRangeException(nameof(connectionPerHost), "value should be between 1 and 64");

            ConnectionPerHost = connectionPerHost;

            return this;
        }

        /// <summary>
        ///  Set the default protocols used by fluxzy 
        /// </summary>
        /// <param name="protocols"></param>
        /// <returns></returns>
        public FluxzySetting SetServerProtocols(SslProtocols protocols)
        {
            ServerProtocols = protocols;
            return this;
        }

        /// <summary>
        /// If false, fluxzy will not check the revocation status of remote certificates.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Skip global ssl decryption. Fluxzy will performs only blind ssl tunneling.
        /// This option may disables several filters and actions based on clear text traffic.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public FluxzySetting SetSkipGlobalSslDecryption(bool value)
        {
            GlobalSkipSslDecryption = value; 

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
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public FluxzySetting SetVerbose(bool value)
        {
            Verbose = value;

            return this;
        }

        /// <summary>
        /// Use the managed BouncyCastle engine
        /// </summary>
        /// <returns></returns>
        public FluxzySetting UseBouncyCastleSslEngine()
        {
            UseBouncyCastle = true;
            return this;
        }

        /// <summary>
        /// Use the default SSL Engine provided by the operating system
        /// </summary>
        /// <returns></returns>
        public FluxzySetting UseOsSslEngine()
        {
            UseBouncyCastle = false;
            return this;
        }

    }
}
