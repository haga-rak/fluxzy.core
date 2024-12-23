// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Newtonsoft.Json;
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
        }

        [JsonProperty("hash")]
        [JsonPropertyName("hash")]
        public string Hash { get; set; }

        [JsonProperty("fingerprint")]
        [JsonPropertyName("fingerprint")]
        public string Fingerprint { get; set; }

        [JsonProperty("ciphers")]
        [JsonPropertyName("ciphers")]
        public string Ciphers { get; set; }

        [JsonProperty("curves")]
        [JsonPropertyName("curves")]
        public string Curves { get; set; }

        [JsonProperty("protocol")]
        [JsonPropertyName("protocol")]
        public string Protocol { get; set; }

        [JsonProperty("user_agent")]
        [JsonPropertyName("user_agent")]
        public string UserAgent { get; set; }


        public string NormalizedFingerPrint {
            get
            {
                return Ja3FingerPrint.Parse(Fingerprint).ToString(true);
            }
        }
    }
}
