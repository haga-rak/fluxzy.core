// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fluxzy.Clients.Ssl;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;
using YamlDotNet.Serialization;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    /// Set a JA3 fingerprint of ongoing connection. 
    /// </summary>
    [ActionMetadata("Set a JA3 fingerprint of ongoing connection.")]
    public class SetJa3FingerPrintAction : Action
    {
        [JsonIgnore]
        [YamlIgnore]
        private TlsFingerPrint?  _fingerPrint;

        public SetJa3FingerPrintAction(string value)
        {
            Value = value;
        }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        [ActionDistinctive]
        public string Value { get; set; }

        public override string DefaultDescription => "Set JA3 fingerprint";

        public override void Init(StartupContext startupContext)
        {
            _fingerPrint = TlsFingerPrint.ParseFromJa3(Value); 
            base.Init(startupContext);
        }

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (!string.IsNullOrWhiteSpace(Value)) {
                context.AdvancedTlsSettings.TlsFingerPrint = _fingerPrint;
            }

            return default;
        }
    }
}
