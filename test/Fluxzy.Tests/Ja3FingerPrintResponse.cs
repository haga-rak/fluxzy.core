// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json.Serialization;
using Fluxzy.Clients.Ssl;

namespace Fluxzy.Tests
{
    public class Ja3FingerPrintResponse
    {
        public Ja3FingerPrintResponse(string hash, 
            string fingerprint, string ciphers, string curves, string protocol, string userAgent)
        {
            Hash = hash;
            Fingerprint = fingerprint;
            Ciphers = ciphers;
            Curves = curves;
            Protocol = protocol;
            UserAgent = userAgent;

            if (Protocol == "TLSv1.3") {
                // uh ja3.zone always return 771,772 for TLSv1.3
                Fingerprint = Fingerprint.Replace("771,", "772,");
            }
        }

        [JsonPropertyName("hash")]
        public string Hash { get; set; }

        [JsonPropertyName("fingerprint")]
        public string Fingerprint { get; set; }

        [JsonPropertyName("ciphers")]
        public string Ciphers { get; set; }

        [JsonPropertyName("curves")]
        public string Curves { get; set; }

        [JsonPropertyName("protocol")]
        public string Protocol { get; set; }

        [JsonPropertyName("user_agent")]
        public string UserAgent { get; set; }


        public string NormalizedFingerPrint {
            get
            {
                return TlsFingerPrint.ParseFromJa3(Fingerprint).ToString(true);
            }
        }
    }
}
