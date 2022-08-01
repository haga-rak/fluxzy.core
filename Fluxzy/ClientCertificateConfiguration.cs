using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace Echoes
{
    public class ClientCertificateConfiguration
    {
        [JsonIgnore]
        private X509Certificate[] _lazyMapping; 

        public List<ClientConfigItem> ClientSettings { get; set; } = new();

        [JsonIgnore]
        public bool HasConfig => ClientSettings != null && ClientSettings.Any();

        internal X509Certificate2 GetCustomClientCertificate(string hostName)
        {
            if (!HasConfig)
                return null;

            return ClientSettings.FirstOrDefault(c => c.Match(hostName))?.Load();
        }

        internal X509Certificate[] ReadAllCertificateMapping()
        {
            if (_lazyMapping != null)
                return _lazyMapping;

            if (ClientSettings == null || !ClientSettings.Any()) 
                return _lazyMapping = Array.Empty<X509Certificate>();

            return _lazyMapping = ClientSettings.Select(c => c.Load()).Where(c => c != null)
                .OfType<X509Certificate>().ToArray();
        }

        public static void GenerateSampleConfig(string fileName)
        {
            var config =  new ClientCertificateConfiguration()
            {
                ClientSettings = new List<ClientConfigItem>()
                {
                    new ClientConfigItem()
                    {
                        HostNames = new List<string>() { "*.mydomain.com", "myotherdomain.*" },
                        CertificatePath = "../path-to-certificate.pfx",
                        Password = "certificate-password"
                    },
                    new ClientConfigItem()
                    {
                        CertificateSerialNumber = "certificate-serial-number-available-on-store",
                        HostNames = new List<string>() { "www.otherhost.io", },
                    }
                }
            };
        }

    }

    public class ClientConfigItem
    {
        [JsonIgnore]
        private readonly IDictionary<string, bool> MatchState = new Dictionary<string, bool>();

        [JsonIgnore]
        private X509Certificate2 _certicateCache = null;

        /// <summary>
        /// List of remote hosts which current certificate will be applied 
        /// </summary>
        public List<string> HostNames { get; set; } = new List<string>();

        /// <summary>
        /// Path to the certificate if any 
        /// </summary>
        public string CertificatePath { get; set; }

        /// <summary>
        /// Password of the certificate if any.  
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// In case the certificate is store on the store, the Serial number can be used to access the
        /// configuration 
        /// </summary>
        public string CertificateSerialNumber { get; set; }

        internal bool Match(string hostName)
        {
            if (MatchState.TryGetValue(hostName, out var state))
            {
                return state; 
            }

            if (HostNames == null)
                return false;

            return HostNames.Any(h => h.WildCardToRegular(hostName));
        }

        internal X509Certificate2 Load()
        {
            if (_certicateCache != null)
                return _certicateCache;

            if (HostNames == null || !HostNames.Any())
                return null; 

            if (!string.IsNullOrWhiteSpace(CertificatePath))
            {
                // Certificate from path 

                return _certicateCache= (Password == null ? new X509Certificate2(File.ReadAllBytes(CertificatePath)) :
                    new X509Certificate2(File.ReadAllBytes(CertificatePath), Password));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(CertificateSerialNumber))
                {
                    throw new InvalidOperationException($"Either {nameof(CertificatePath)} or {nameof(CertificateSerialNumber)} must be defined " +
                                                        $"when configuring a client certificate for host {string.Join(" ", HostNames)}");
                }


                using (X509Store store = new X509Store(StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var certificateCollection = store.Certificates.Find(X509FindType.FindBySerialNumber, CertificateSerialNumber, false);

                    if (certificateCollection.Count ==0)
                        throw new InvalidOperationException($"Certificate with serial number {CertificateSerialNumber} was not found or cannot be accessed for " +
                                                            $"client certificate configuration {string.Join(" ", HostNames)}");

                    return _certicateCache = certificateCollection[0];
                }

                return null;
            }
                // Certificate from my store 

        }

    }
}