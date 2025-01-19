// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Clients.Headers;
using Fluxzy.Clients.Ssl;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Rules.Filters;
using Org.BouncyCastle.Tls;
using YamlDotNet.Serialization;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Impersonate a browser or client by changing the TLS fingerprint, HTTP/2 settings and headers.
    /// </summary>
    [ActionMetadata("Impersonate a browser or client by changing the TLS fingerprint, HTTP/2 settings and headers.")]
    public class ImpersonateAction : Action
    {
        [JsonIgnore]
        [YamlIgnore]
        private ImpersonateConfiguration? _configuration;

        [JsonIgnore]
        [YamlIgnore]
        private TlsFingerPrint? _fingerPrint;

        /// <summary>
        /// </summary>
        /// <param name="nameOrConfigFile"></param>
        public ImpersonateAction(string nameOrConfigFile)
        {
            NameOrConfigFile = nameOrConfigFile;
        }

        /// <summary>
        ///     Name or config file
        /// </summary>
        [ActionDistinctive]
        public string NameOrConfigFile { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => $"Impersonate {NameOrConfigFile}";

        public override void Init(StartupContext startupContext)
        {
            base.Init(startupContext);

            _configuration = ImpersonateConfigurationManager.Instance.LoadConfiguration(NameOrConfigFile);

            if (_configuration == null) {
                throw new FluxzyException($"Impersonate configuration '{NameOrConfigFile}' not found.");
            }

            _fingerPrint = TlsFingerPrint.ParseFromJa3(
                _configuration.NetworkSettings.Ja3FingerPrint,
                _configuration.NetworkSettings.GreaseMode,
                signatureAndHashAlgorithms:
                _configuration.NetworkSettings
                              .SignatureAlgorithms?.Select(s =>
                                  SignatureAndHashAlgorithm.GetInstance(SignatureScheme.GetHashAlgorithm(s),
                                      SignatureScheme.GetSignatureAlgorithm(s))
                              ).ToList(),
                earlyShardGroups: _configuration.NetworkSettings.EarlySharedGroups
            );
        }

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (_configuration != null) {
                context.AdvancedTlsSettings.TlsFingerPrint = _fingerPrint;

                var streamSetting = new H2StreamSetting();

                if (_configuration.H2Settings.RemoveDefaultValues) {
                    streamSetting.AdvertiseSettings.Clear();
                }

                foreach (var setting in _configuration.H2Settings.Settings) {
                    streamSetting.AdvertiseSettings.Add(setting.Identifier);
                    streamSetting.SetSetting(setting.Identifier, setting.Value);
                }

                context.AdvancedTlsSettings.H2StreamSetting = streamSetting;

                var existingHeaders = exchange?.GetRequestHeaders().Select(s => s.Name)
                                              .ToHashSet(SpanCharactersIgnoreCaseComparer.Default);

                if (existingHeaders != null) {
                    foreach (var header in _configuration.Headers) {
                        if (header.SkipIfExists) {
                            if (!existingHeaders.Contains(header.Name.AsMemory())) {
                                context.RequestHeaderAlterations.Add(new HeaderAlterationAdd(
                                    header.Name, header.Value));
                            }
                        }
                        else {
                            context.RequestHeaderAlterations.Add(new HeaderAlterationReplace(
                                header.Name, header.Value, true));
                        }
                    }
                }
            }

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample("Impersonate CHROME 131 on Windows",
                new ImpersonateAction("Chrome_Windows_131")
            );
        }

        public override IEnumerable<ValidationResult> Validate(FluxzySetting setting, Filter filter)
        {
            if (setting.UseBouncyCastle) {
                yield break;
            }

            yield return new ValidationResult(
                ValidationRuleLevel.Warning,
                "Impersonate action requires BouncyCastle to be enabled.",
                FriendlyName);

            foreach (var result in base.Validate(setting, filter)) {
                yield return result;
            }
        }
    }
}
