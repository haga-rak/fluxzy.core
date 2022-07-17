using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using Echoes.Core;

namespace Echoes
{
    public class ProxyStartupSetting
    { 

        private ProxyStartupSetting()
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

        /// <summary>
        /// Verbose mode 
        /// </summary>
        public bool Verbose { get; internal set; } = false;

        /// <summary>
        /// If set to true. All ssl connection will be tunneled disregards of TunneledOnlyHosts
        /// </summary>
        public bool SkipSslDecryption { get; internal set; } = false;

        /// <summary>
        /// Number of concurrent connection per host maintained by the connection pool excluding websocket connections. Default value is 8.
        /// </summary>
        public int ConnectionPerHost { get; internal set; } = 8;

        /// <summary>
        /// Number of anticipated connection per host
        /// </summary>
        public int AnticipatedConnectionPerHost { get; internal set; } = 0;

        /// <summary>
        /// When set to true. Echoes will be set as system proxy when started. 
        /// </summary>
        public bool RegisterAsSystemProxy { get; internal set; } = false;


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
        /// List of host where encryption is not set.
        /// </summary>
        public HashSet<string> TunneledOnlyHosts { get; internal set; }  = new HashSet<string>();

        /// <summary>
        /// The certificate used 
        /// </summary>
        public CertificateConfiguration CertificateConfiguration { get; internal set; } = new CertificateConfiguration(null, string.Empty);

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


        /// <summary>
        /// 
        /// </summary>
        public ClientCertificateConfiguration ClientCertificateConfiguration { get; internal set; }


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
        /// Set hosts that bypass the proxy
        /// </summary>
        /// <param name="hosts"></param>
        /// <returns></returns>
        public ProxyStartupSetting SetByPassedHosts(params string[] hosts)
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
         public ProxyStartupSetting SetArchivingPolicy(ArchivingPolicy archivingPolicy)
         {
             ArchivingPolicy = archivingPolicy ?? throw new ArgumentNullException(nameof(archivingPolicy));
             return this; 
         }

        /// <summary>
        /// Add hosts that echoes should not decrypt
        /// </summary>
        /// <param name="hosts"></param>
        /// <returns></returns>
        public ProxyStartupSetting AddTunneledHosts(params string[] hosts)
        {
            foreach (var host in hosts.Where(h => !string.IsNullOrWhiteSpace(h)))
            {
                TunneledOnlyHosts.Add(host);
            }

            return this;
        }
        
        public ProxyStartupSetting AddBoundAddress(string boundAddress, int port, bool ? @default = null)
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
        
        public ProxyStartupSetting SetBoundAddress(string boundAddress, int port)
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

        public ProxyStartupSetting SetConnectionPerHost(int connectionPerHost)
        {
            if (connectionPerHost < 1 || connectionPerHost >= 64)
            {
                throw new ArgumentOutOfRangeException(nameof(connectionPerHost), "value should be between 1 and 64");
            }

            ConnectionPerHost = connectionPerHost;
            return this; 
        }

        public ProxyStartupSetting SetAnticipatedConnectionPerHost(int anticipatedConnectionPerHost)
        {
            if (anticipatedConnectionPerHost < 1 || anticipatedConnectionPerHost >= 32)
            {
                throw new ArgumentOutOfRangeException(nameof(anticipatedConnectionPerHost), "value should be between 1 and 32");
            }

            AnticipatedConnectionPerHost = anticipatedConnectionPerHost;
            return this; 
        }

        public ProxyStartupSetting SetAsSystemProxy(bool value)
        {
            RegisterAsSystemProxy = value;
            return this; 
        }

        public ProxyStartupSetting SetThrottleKoPerSecond(int value)
        {
            // To do controller supérieur à une valeur minimum 

            if (value < 8)
            {
                throw new ArgumentException("value cannot be less than 8");
            }

            ThrottleKBytePerSecond = value;
            return this; 
        }

        public ProxyStartupSetting SetClientCertificateConfiguration(
            ClientCertificateConfiguration clientCertificateConfiguration)
        {
            ClientCertificateConfiguration = clientCertificateConfiguration;

            clientCertificateConfiguration.ReadAllCertificateMapping();

            return this;
        }

        public ProxyStartupSetting SetClientCertificateConfiguration(
             string fileName)
        {
            try
            {
                ClientCertificateConfiguration =
                    JsonSerializer.Deserialize<ClientCertificateConfiguration>(File.ReadAllText(fileName), 
                        StartupConfigSetting.JsonOptions);

            }
            catch (Exception ex)
            {
                throw new ArgumentException($"An error occurs when trying to read client certificate configuration from {fileName} : {ex.Message}");
            }

            return this;
        }


        /// <summary>
        /// Set the interval on which the throttle setting will be adjusted. Default value is 50ms
        /// </summary>
        /// <returns></returns>
        public ProxyStartupSetting SetThrottleIntervalCheck(TimeSpan delay)
        {
            if (delay < TimeSpan.FromMilliseconds(40) || delay > TimeSpan.FromSeconds(2))
                throw new ArgumentException($"{nameof(delay)} must be between than 40ms and 2s");

            ThrottleIntervalCheck = delay;
            return this;
        }

        public ProxyStartupSetting SetServerProtocols(SslProtocols protocols)
        {
            ServerProtocols = protocols;
            return this;
        }

        public ProxyStartupSetting SetCheckCertificateRevocation(bool value)
        {
            CheckCertificateRevocation = value;

            return this; 
        }
        
        public ProxyStartupSetting SetAutoInstallCertificate(bool value)
        {
            AutoInstallCertificate = value;
            return this; 
        }

        public ProxyStartupSetting SetSkipSslDecryption(bool value)
        {
            SkipSslDecryption = value;
            return this; 
        }

        public ProxyStartupSetting SetVerbose(bool value)
        {
            Verbose = value;
            return this; 
        }
        
        /// <summary>
        /// Change the default certificate used by fiddler
        /// </summary>
        /// <param name="file"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public ProxyStartupSetting SetSecureCertificate(byte[] file, string password)
        {
            try
            {
                using (var cer = new X509Certificate2(file, password))
                {
                    if (!cer.HasPrivateKey)
                    {
                        throw new ArgumentException($"The provide certificate must contains a private key");
                    }
                }
            }
            catch (Exception)
            {
                throw new ArgumentException("Error when trying to load certificate");
            }

            CertificateConfiguration = new CertificateConfiguration(file, password);

            return this;
        }

        public ProxyStartupSetting SetDisableCertificateCache(bool value)
        {
            DisableCertificateCache = value; 
            return this; 
        }

        /// <summary>
        /// Get the default setting 
        /// </summary>
        /// <returns></returns>
        public static ProxyStartupSetting CreateDefault()
        {
            return new ProxyStartupSetting()
            {
                ConnectionPerHost =  8, 
                AnticipatedConnectionPerHost = 3
            }.SetBoundAddress("127.0.0.1", 44344);
        }

        internal IConsoleOutput GetDefaultOutput()
        {
           return new DefaultConsoleOutput(Verbose ? Console.Out : null);
        }
    }


    public class ProxyBindPoint : IEquatable<ProxyBindPoint>
    {
        public bool Equals(ProxyBindPoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Address == other.Address && Port == other.Port && Default == other.Default;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ProxyBindPoint)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Address, Port, Default);
        }

        public ProxyBindPoint(string address, int port)
        {
            Address = address;
            Port = port;
        }

        public ProxyBindPoint(string address, int port, bool @default)
        {
            Address = address;
            Port = port;
            Default = @default;
        }

        /// <summary>
        /// The address on with the proxy will listen to 
        /// </summary>
        public string Address { get;  }

        /// <summary>
        /// Port number 
        /// </summary>
        public int Port { get;  }

        /// <summary>
        /// Whether this setting is the default bound address port. When true,
        /// this setting will be choosed as system proxy
        /// </summary>
        public bool Default { get; set; }
    }


    public enum ArchivingPolicyType
    {
        // The proxy 
        None = 0 , 
        // Content is set on server  
        Directory
    }

    public class ArchivingPolicy
    {
        [JsonConstructor]
        internal ArchivingPolicy()
        {

        }

        public ArchivingPolicyType Type { get; internal set; }

        public string Directory { get; internal set; }

        public static ArchivingPolicy None { get; } = new();

        public static ArchivingPolicy CreateFromDirectory(string path)
        {
            return new ArchivingPolicy()
            {
                Type = ArchivingPolicyType.Directory,
                Directory = path
            }; 
        }


    }
    
}