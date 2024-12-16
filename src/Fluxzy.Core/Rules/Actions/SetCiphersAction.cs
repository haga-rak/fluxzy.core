// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fluxzy.Clients.Ssl;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    /// Force usage of a specific cipher suite.
    /// </summary>
    [ActionMetadata("Force usage of a specific cipher suite")]
    public class SetCiphersAction : Action
    {

        [JsonConstructor]
        public SetCiphersAction(List<string> ciphers)
        {
            Ciphers = ciphers;
        }

        [JsonConstructor]
        public SetCiphersAction(params string[] ciphers)
         : this (ciphers.ToList())
        {
        }
        
        [ActionDistinctive]
        public List<string>? Ciphers { get; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => $"Force Ciphers";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (Ciphers != null) {
                context.CipherConfiguration = new SslConnectionBuilderOptionsCipherConfiguration(Ciphers);
            }

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample("Change ciphers from a predefined list.", 
                new SetCiphersAction(new List<string> {
                    "TLS_AES_128_GCM_SHA256",
                    "TLS_AES_256_GCM_SHA384",
                }));
        }
    }
}
