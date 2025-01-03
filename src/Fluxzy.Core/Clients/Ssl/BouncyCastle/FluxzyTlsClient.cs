// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using Org.BouncyCastle.Tls;

#pragma warning disable SYSLIB0039

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    internal class FluxzyTlsClient : DefaultTlsClient
    {
        private readonly IReadOnlyCollection<SslApplicationProtocol>_applicationProtocols;
        private readonly FluxzyCrypto _crypto;
        private readonly FingerPrintTlsExtensionsEnforcer _fingerPrintEnforcer;
        private readonly SslProtocols _sslProtocols;
        private readonly string _targetHost;
        private readonly TlsAuthentication _tlsAuthentication;
        private readonly Ja3FingerPrint? _fingerPrint;
        private readonly List<ServerName> _serverNames;
        private readonly IList<ProtocolName> _protocolNames;

        public FluxzyTlsClient(
            SslConnectionBuilderOptions builderOptions,
            TlsAuthentication tlsAuthentication,
            FluxzyCrypto crypto, 
            FingerPrintTlsExtensionsEnforcer fingerPrintEnforcer)
            : base(crypto)
        {
            _targetHost = builderOptions.TargetHost;
            _sslProtocols = builderOptions.EnabledSslProtocols;
            _applicationProtocols = builderOptions.ApplicationProtocols;
            _tlsAuthentication = tlsAuthentication;
            _crypto = crypto;
            _fingerPrintEnforcer = fingerPrintEnforcer;
            _fingerPrint = builderOptions.AdvancedTlsSettings?.Ja3FingerPrint;
            _serverNames = new List<ServerName>() { new ServerName(0, 
                Encoding.UTF8.GetBytes(builderOptions.TargetHost))
            };
            //this.GetKeyExchangeFactory()

            _protocolNames = InternalGetProtocolNames();
        }

        public override void Init(TlsClientContext context)
        {
            base.Init(context);

            _crypto.UpdateContext(context);

            if (_fingerPrint != null)
            {
                m_cipherSuites = _fingerPrint.EffectiveCiphers;
            }

            m_protocolVersions = InternalGetProtocolVersions();
            
        }

        public override IList<int> GetEarlyKeyShareGroups()
        {
            if (_fingerPrint != null) {
                var allowedValues = new HashSet<int>() {
                    NamedGroup.X25519MLKEM768, NamedGroup.x25519,
                    NamedGroup.grease
                };

                return _fingerPrint.EffectiveSupportGroups.Where(r => allowedValues.Contains(r)).ToList();
            }

            return base.GetEarlyKeyShareGroups();
        }

        protected override IList<int> GetSupportedGroups(IList<int> namedGroupRoles)
        {
            if (_fingerPrint != null)
            {
                return _fingerPrint.EffectiveSupportGroups;
            }

            return base.GetSupportedGroups(namedGroupRoles);
        }

        protected override IList<ServerName> GetSniServerNames()
        {
            return _serverNames;
        }
        
        public override IList<SupplementalDataEntry> GetClientSupplementalData()
        {
            var entry = new SupplementalDataEntry(0, Encoding.UTF8.GetBytes("Ja3"));
            return base.GetClientSupplementalData();
        }

        public override IDictionary<int, byte[]> GetClientExtensions()
        {
            var baseExtensions = base.GetClientExtensions();

            if (_fingerPrint != null)
            {
                var result = _fingerPrintEnforcer.PrepareExtensions(baseExtensions,
                    _fingerPrint, _targetHost, m_protocolVersions);

                return result;
            }

            return baseExtensions;
        }

        public override TlsAuthentication GetAuthentication()
        {
            return _tlsAuthentication;
        }
        
        protected override IList<ProtocolName> GetProtocolNames()
        {
            return _protocolNames;
        }

        private IList<ProtocolName> InternalGetProtocolNames()
        {
            var result = new List<ProtocolName>();

            if (!_applicationProtocols.Any()) {
                return base.GetProtocolNames();
            }

            foreach (var applicationProtocol in _applicationProtocols) {
                if (applicationProtocol.Protocol.Equals(SslApplicationProtocol.Http11.Protocol))
                {
                    result.Add(ProtocolName.Http_1_1);
                }

                if (applicationProtocol.Protocol.Equals(SslApplicationProtocol.Http2.Protocol))
                {
                    result.Add(ProtocolName.Http_2_Tls);
                }
            }

            return result;
        }
        
        private ProtocolVersion[] InternalGetProtocolVersions()
        {
            if (_fingerPrint != null) 
            {
                var version = ProtocolVersionHelper.GetFromRawValue(_fingerPrint.ProtocolVersion);

                if (version.IsEarlierVersionOf(ProtocolVersion.TLSv12))
                    return version.Only();

                if (_fingerPrint.GreaseMode) {
                    // those allocation are shity
                    return new[] { ProtocolVersion.Grease }.Concat(version.DownTo(ProtocolVersion.TLSv12))
                                                           .ToArray();
                }

                return version.DownTo(ProtocolVersion.TLSv12);
            }

            var listProtocolVersion = new List<ProtocolVersion>();

            if (SslProtocols.None == _sslProtocols) {
                return base.GetSupportedVersions();
            }

            if (_sslProtocols.HasFlag(SslProtocols.Tls)) {
                listProtocolVersion.Add(ProtocolVersion.TLSv10);
            }

            if (_sslProtocols.HasFlag(SslProtocols.Tls11)) {
                listProtocolVersion.Add(ProtocolVersion.TLSv11);
            }

            if (_sslProtocols.HasFlag(SslProtocols.Tls12)) {
                listProtocolVersion.Add(ProtocolVersion.TLSv12);
            }

#if NET6_0_OR_GREATER
            if (_sslProtocols.HasFlag(SslProtocols.Tls13)) {
                listProtocolVersion.Add(ProtocolVersion.TLSv13);
            }
#endif

            return listProtocolVersion.ToArray();
        }
    }

    internal static class ProtocolVersionHelper
    {
        private static readonly ProtocolVersion[] SupportedVersions = new ProtocolVersion[]
        {
            ProtocolVersion.TLSv10,
            ProtocolVersion.TLSv11,
            ProtocolVersion.TLSv12,
            ProtocolVersion.TLSv13
        };

        public static ProtocolVersion GetFromRawValue(int protocolVersion)
        {
            var result =  SupportedVersions.FirstOrDefault(v => (int)v.FullVersion == protocolVersion);

            if (result == null)
            {
                throw new ArgumentException($"Invalid protocol version {protocolVersion}");
            }

            return result;
        }
    }
}
