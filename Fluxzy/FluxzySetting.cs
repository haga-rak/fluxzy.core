using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text.Json.Serialization;
using Fluxzy.Core;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Fluxzy
{
    public class FluxzySetting
    {
        [JsonConstructor]
        public FluxzySetting()
        {

        }

        /// <summary>
        /// Proxy listen address 
        /// </summary>
        public HashSet<ProxyBindPoint> BoundPoints { get; internal set; } = new();

        /// <summary>
        /// Returns a friendly description of the bound points
        /// </summary>
        public string BoundPointsDescription
        {
            get
            {
                return string.Join(", ", BoundPoints
                    .OrderByDescending(d => d.Default)
                    .Select(s => $"[{s.Address}:{s.Port}]"));
            }
        }

        internal int ExchangeStartIndex { get; set; } = 0; 

        /// <summary>
        /// Verbose mode 
        /// </summary>
        public bool Verbose { get; internal set; } = false;

        /// <summary>
        /// Number of concurrent connection per host maintained by the connection pool excluding websocket connections. Default value is 8.
        /// </summary>
        public int ConnectionPerHost { get; internal set; } = 8;

        /// <summary>
        /// Number of anticipated connection per host
        /// </summary>
        public int AnticipatedConnectionPerHost { get; internal set; } = 0;
        
        /// <summary>
        /// Download bandwidth in KiloByte  (Byte = 8bits) per second. Default value is 0 which means no throttling.
        /// </summary>
        public int ThrottleKBytePerSecond { get; internal set; } = 0;

        
        public SslProtocols ServerProtocols { get; internal set; } = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;

        /// <summary>
        /// Time interval on which the bandwidth throttle will be adjusted. Default value is 50ms.
        /// </summary>
        public TimeSpan ThrottleIntervalCheck { get; internal set; } = TimeSpan.FromMilliseconds(50);

        /// <summary>
        /// The CA certificate for the man on the middle 
        /// </summary>
        public Certificate CaCertificate { get; internal set; } = Certificate.UseDefault();

        /// <summary>
        /// The default certificate cache directory used by echoes proxy
        /// </summary>

        public string CertificateCacheDirectory { get; internal set; } = "%appdata%/.echoes/cert-caches";

        /// <summary>
        /// When set to true, echoes will automaticall install default certificate when starting.
        /// </summary>
        public bool AutoInstallCertificate { get; internal set; } = true; 

        /// <summary>
        /// Check whether server certificate is valid. Default value is true
        /// </summary>
        public bool CheckCertificateRevocation { get; internal set; } = true;


        /// <summary>
        /// Do not use certificate cache. Regen certificate whenever asked 
        /// </summary>
        public bool DisableCertificateCache { get; internal set; } = false;

        
        public IReadOnlyCollection<string> ByPassHost { get; internal set; } = new List<string>() { "localhost", "127.0.0.1" };


        /// <summary>
        /// Max header length used by the browser 
        /// </summary>
        public int MaxHeaderLength { get; set; } = 16384;

        /// <summary>
        /// 
        /// </summary>
        public ArchivingPolicy ArchivingPolicy { get; internal set; } = ArchivingPolicy.None;

        /// <summary>
        /// Global alteration rules 
        /// </summary>
        public List<Rule> AlterationRules { get; set; } = new();

        /// <summary>
        /// Set hosts that bypass the proxy
        /// </summary>
        /// <param name="hosts"></param>
        /// <returns></returns>
        public FluxzySetting SetByPassedHosts(params string[] hosts)
        {
            ByPassHost = new ReadOnlyCollection<string>(hosts.Where(h => !string.IsNullOrWhiteSpace(h)).ToList()) ;
            return this; 
        }

        /// <summary>
        /// Set archiving policy
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
        /// Add hosts that echoes should not decrypt
        /// </summary>
        /// <param name="hosts"></param>
        /// <returns></returns>
        public FluxzySetting AddTunneledHosts(params string[] hosts)
        {
            foreach (var host in hosts.Where(h => !string.IsNullOrWhiteSpace(h)))
            {
                AlterationRules.Add(new Rule(
                    new SkipSslTunnelingAction(),
                    new HostFilter(host, StringSelectorOperation.Exact)));
            }

            return this;
        }
        

        public FluxzySetting AddBoundAddress(string boundAddress, int port, bool ? @default = null)
        {
            if (!IPAddress.TryParse(boundAddress, out _))
            {
                throw new ArgumentException($"{boundAddress} is not a valid IP address");
            }

            if (port < 1 || port >= ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(port), $"port should be between 1 and {ushort.MaxValue}");
            }
            
            var isDefault = @default ?? BoundPoints.All(e => !e.Default);
            BoundPoints.Add(new ProxyBindPoint(boundAddress, port, isDefault)); 
            
            return this; 
        }
        

        public FluxzySetting SetBoundAddress(string boundAddress, int port)
        {
            if (!IPAddress.TryParse(boundAddress, out _))
            {
                throw new ArgumentException($"{boundAddress} is not a valid IP address");
            }

            if (port < 1 || port >= ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(port), $"port should be between 1 and {ushort.MaxValue}");
            }

            BoundPoints.Clear();
            BoundPoints.Add(new ProxyBindPoint(boundAddress, port, true)); 
            
            return this; 
        }

        public FluxzySetting SetConnectionPerHost(int connectionPerHost)
        {
            if (connectionPerHost < 1 || connectionPerHost >= 64)
            {
                throw new ArgumentOutOfRangeException(nameof(connectionPerHost), "value should be between 1 and 64");
            }

            ConnectionPerHost = connectionPerHost;
            return this; 
        }
        
        public FluxzySetting SetThrottleKoPerSecond(int value)
        {
            // To do controller supérieur à une valeur minimum 

            if (value < 8)
            {
                throw new ArgumentException("value cannot be less than 8");
            }

            ThrottleKBytePerSecond = value;
            return this; 
        }

        public FluxzySetting SetClientCertificateOnHost(string host, Certificate certificate)
        {
            AlterationRules.Add(new Rule(new SetClientCertificateAction(certificate), new HostFilter(host)));
            return this;
        }

        /// <summary>
        /// 
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


        /// <summary>
        /// Set the interval on which the throttle setting will be adjusted. Default value is 50ms
        /// </summary>
        /// <returns></returns>
        public FluxzySetting SetThrottleIntervalCheck(TimeSpan delay)
        {
            if (delay < TimeSpan.FromMilliseconds(40) || delay > TimeSpan.FromSeconds(2))
                throw new ArgumentException($"{nameof(delay)} must be between than 40ms and 2s");

            ThrottleIntervalCheck = delay;
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

        public FluxzySetting SetVerbose(bool value)
        {
            Verbose = value;
            return this; 
        }
        
        /// <summary>
        /// Change the default certificate used by fluxzy
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
        /// Get the default setting 
        /// </summary>
        /// <returns></returns>
        public static FluxzySetting CreateDefault()
        {
            return new FluxzySetting()
            {
                ConnectionPerHost =  8, 
                AnticipatedConnectionPerHost = 3
            }
                .SetBoundAddress("127.0.0.1", 44344);
        }

        internal IConsoleOutput GetDefaultOutput()
        {
           return new DefaultConsoleOutput(Verbose ? Console.Out : null);
        }
    }


}