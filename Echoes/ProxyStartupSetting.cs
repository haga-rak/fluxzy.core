using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Echoes.Core;
using Newtonsoft.Json;

namespace Echoes
{
    public class ProxyStartupSetting
    {
        private ProxyStartupSetting()
        {

        }

        /// <summary>
        /// The address on which the proxy is listening. Default value is 127.0.0.1. 0.0.0.0 to listen to all network interfaces
        /// </summary>
        [JsonProperty]
        public string BoundAddress { get; internal set; } = "127.0.0.1";


        [JsonProperty]
        public bool Verbose { get; internal set; } = false;

        /// <summary>
        /// The proxy port. Default value is ...
        /// </summary>
        [JsonProperty]
        public int ListenPort { get; internal set; } = 44344;

        /// <summary>
        /// If set to true. All ssl connection will be tunneled disregards of TunneledOnlyHosts
        /// </summary>
        [JsonProperty]
        public bool SkipSslDecryption { get; internal set; } = false;

        /// <summary>
        /// Number of concurrent connection per host maintained by the connection pool excluding websocket connections. Default value is 8.
        /// </summary>
        [JsonProperty]
        public int ConnectionPerHost { get; internal set; } = 8;

        /// <summary>
        /// Number of anticipated connection per host
        /// </summary>
        [JsonProperty]
        public int AnticipatedConnectionPerHost { get; internal set; } = 0;

        /// <summary>
        /// When set to true. Echoes will be set as system proxy when started. 
        /// </summary>
        [JsonProperty]
        public bool RegisterAsSystemProxy { get; internal set; } = false;

        /// <summary>
        /// Above this value, the response body is cached to a temp file. Default value is 0 which means no temp file caching.
        /// </summary>
        [JsonProperty]
        public long TempStorageSizeLimit { get; internal set; } = 0;

        /// <summary>
        /// Location of temp storage 
        /// </summary>
        [JsonProperty]
        public string TempStorage { get; internal set; }



        /// <summary>
        /// Download bandwidth in KiloByte  (Byte = 8bits) per second. Default value is 0 which means no throttling.
        /// </summary>
        [JsonProperty]
        public int ThrottleKBytePerSecond { get; internal set; } = 0;


        [JsonProperty]
        public SslProtocols ServerProtocols { get; internal set; } = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;

        /// <summary>
        /// Time interval on which the bandwidth throttle will be adjusted. Default value is 50ms.
        /// </summary>
        [JsonProperty]
        public TimeSpan ThrottleIntervalCheck { get; internal set; } = TimeSpan.FromMilliseconds(50);

        /// <summary>
        /// List of host where encryption is not set.
        /// </summary>
        [JsonProperty]
        public HashSet<string> TunneledOnlyHosts { get; internal set; }  = new HashSet<string>();

        /// <summary>
        /// The certificate used 
        /// </summary>
        [JsonProperty]
        public CertificateConfiguration CertificateConfiguration { get; internal set; } = new CertificateConfiguration(null, string.Empty);

        /// <summary>
        /// The default certificate cache directory used by echoes proxy
        /// </summary>

        [JsonProperty]
        public string CertificateCacheDirectory { get; internal set; } = ".echoes/cert-caches";

        /// <summary>
        /// When set to true, echoes will automaticall install default certificate when starting.
        /// </summary>
        [JsonProperty]
        public bool AutoInstallCertificate { get; internal set; } = true; 

        /// <summary>
        /// Check whether server certificate is valid. Default value is true
        /// </summary>
        public bool CheckCertificateRevocation { get; internal set; } = true;


        public bool DisableCertificateCache { get; internal set; } = false;


        public ClientCertificateConfiguration ClientCertificateConfiguration { get; internal set; }


        public IReadOnlyCollection<string> ByPassHost { get; internal set; } = new List<string>() { "localhost", "127.0.0.1" };


        public int MaxHeaderLength { get; set; } = 8192;


        public ProxyStartupSetting SetByPassedHosts(params string[] hosts)
        {
            ByPassHost = new ReadOnlyCollection<string>(hosts.Where(h => !string.IsNullOrWhiteSpace(h)).ToList()) ;
            return this; 
        }

        public ProxyStartupSetting AddTunneledHosts(params string[] hosts)
        {
            foreach (var host in hosts.Where(h => !string.IsNullOrWhiteSpace(h)))
            {
                TunneledOnlyHosts.Add(host);
            }

            return this;
        }
        
        public ProxyStartupSetting SetBoundAddress(string boundAddress)
        {
            if (!IPAddress.TryParse(boundAddress, out _))
            {
                throw new ArgumentException($"{boundAddress} is not a valid IP address");
            }

            BoundAddress = boundAddress;
            return this; 
        }

        public ProxyStartupSetting SetListenPort(int port)
        {

            if (port < 1 || port >= ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(port), $"port should be between 1 and {ushort.MaxValue}");
            }

            ListenPort = port;
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
                ClientCertificateConfiguration = JsonConvert.DeserializeObject<ClientCertificateConfiguration>(
                    File.ReadAllText(fileName));


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
            // To do controller supérieur à une valeur minimum 

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
                BoundAddress = "127.0.0.1",
                ListenPort = 44344,
                ConnectionPerHost =  8, 
                AnticipatedConnectionPerHost = 3
            };
        }



        /// <summary>
        /// TODO : Move to another class as it's another role
        /// </summary>
        /// <returns></returns>
        internal bool ShouldSkipDecryption(string hostName, int port)
        {
            if (SkipSslDecryption)
                return true;

            if (TunneledOnlyHosts == null)
                return false;

            return TunneledOnlyHosts.Contains(hostName);
        }

        internal IConsoleOutput GetDefaultOutput()
        {
           return new DefaultConsoleOutput(Verbose ? Console.Out : null);
        }
    }

    public class CertificateConfiguration
    {
        public CertificateConfiguration(byte[] rawCertificate, string password)
        {
            if (rawCertificate == null)
                return; 

            Certificate = new X509Certificate2(rawCertificate, password);
        }


        internal X509Certificate2 Certificate { get; set; }


        public bool DefaultConfig
        {
            get
            {
                return Certificate == null;
            }
        }
    }
}