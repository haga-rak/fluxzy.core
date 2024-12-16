// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using Org.BouncyCastle.Tls;

namespace Fluxzy.Clients.Ssl
{
    public class SslConnectionBuilderOptionsCipherConfiguration {

        public SslConnectionBuilderOptionsCipherConfiguration(IReadOnlyCollection<string> cipherNames)
        {
            var cipherList = new List<int>();
            
            foreach (var cipherName in cipherNames)
            {
                if (int.TryParse(cipherName, out var value)) {
                    cipherList.Add(value);
                    continue; 
                }

                if (Enum.TryParse<CipherSuiteNames>(cipherName, true, out var result)) {

                    cipherList.Add((int)result);
                    continue;
                }

                throw new ArgumentException($"Invalid cipher name {cipherName}");
            }

            BouncyCastleCiphers = cipherList.ToArray();
        }


        public int[] BouncyCastleCiphers { get; }

    }
}
