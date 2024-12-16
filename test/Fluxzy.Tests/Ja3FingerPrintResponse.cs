// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fluxzy.Tests
{
    public class Ja3FingerPrintRepository
    {
        // Create with the following values Chrome 131
        //   "ja3": "772,4865-4866-4867-49195-49199-49196-49200-52393-52392-49171-49172-156-157-47-53,13-5-10-17513-45-35-43-27-18-51-65037-0-65281-16-11-23,4588-29-23-24,0",
        // "ja3n": "772,4865-4866-4867-49195-49199-49196-49200-52393-52392-49171-49172-156-157-47-53,0-5-10-11-13-16-18-23-27-35-43-45-51-17513-65037-65281,4588-29-23-24,0",
        // "ja3_digest": "76a68cc203297656d66528ba02d7ab37",
        // "ja3n_digest": "e56af42b95984dd3ca73239168f7b316",

        public static Dictionary<string, Ja3FingerPrintResponse> FingerPrints { get; } = new(StringComparer.OrdinalIgnoreCase);

        static Ja3FingerPrintRepository()
        {
            // Add Chrome 131

            FingerPrints.Add("Chrome_131", 
                new Ja3FingerPrintResponse(
                    "772,4865-4866-4867-49195-49199-49196-49200-52393-52392-49171-49172-156-157-47-53,13-5-10-17513-45-35-43-27-18-51-65037-0-65281-16-11-23,4588-29-23-24,0",
                    "772,4865-4866-4867-49195-49199-49196-49200-52393-52392-49171-49172-156-157-47-53,0-5-10-11-13-16-18-23-27-35-43-45-51-17513-65037-65281,4588-29-23-24,0",
                    "76a68cc203297656d66528ba02d7ab37",
                    "e56af42b95984dd3ca73239168f7b316")
                );

        }

    }

    public class Ja3FingerPrintResponse
    {
        public Ja3FingerPrintResponse(string ja3, string ja3N, string ja3Digest, string ja3NDigest)
        {
            Ja3 = ja3;
            Ja3n = ja3N;
            Ja3Digest = ja3Digest;
            Ja3nDigest = ja3NDigest;
        }

        [JsonPropertyName("ja3")]
        public string Ja3 { get;  }

        [JsonPropertyName("ja3n")]
        public string Ja3n { get; }

        [JsonPropertyName("ja3_digest")]
        public string Ja3Digest { get; }

        [JsonPropertyName("ja3n_digest")]
        public string Ja3nDigest { get; }
    }
}
