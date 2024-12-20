// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using Org.BouncyCastle.Tls;

#pragma warning disable SYSLIB0039

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    internal class FluxzyTlsClient : DefaultTlsClient
    {
        private readonly IReadOnlyCollection<SslApplicationProtocol>_applicationProtocols;
        private readonly FluxzyCrypto _crypto;
        private readonly SslProtocols _sslProtocols;
        private readonly string _targetHost;
        private readonly TlsAuthentication _tlsAuthentication;
        private readonly int[]?  _cipherSuites;
        private readonly Ja3FingerPrint? _fingerPrint;

        public FluxzyTlsClient(
            SslConnectionBuilderOptions builderOptions,
            TlsAuthentication tlsAuthentication,
            FluxzyCrypto crypto)
            : base(crypto)
        {
            _targetHost = builderOptions.TargetHost;
            _sslProtocols = builderOptions.EnabledSslProtocols;
            _applicationProtocols = builderOptions.ApplicationProtocols;
            _tlsAuthentication = tlsAuthentication;
            _crypto = crypto;
            _fingerPrint = builderOptions.AdvancedTlsSettings?.Ja3FingerPrint; 
            _cipherSuites = _fingerPrint?.Ciphers.ToArray(); // TODO: Uncessary allocation here
        }

        public override void Init(TlsClientContext context)
        {
            base.Init(context);
            _crypto.UpdateContext(context);
        }

        public override IDictionary<int, byte[]> GetClientExtensions()
        {
            var baseExtensions = base.GetClientExtensions();

            var pv = m_protocolVersions;

            if (_fingerPrint != null)
            {
                var result =  ClientExtensionHelper.AdjustClientExtensions(
                    baseExtensions, _fingerPrint, _targetHost, m_protocolVersions);

                return result;
            }

            return baseExtensions;
        }

        protected override IList<int> GetSupportedGroups(IList<int> namedGroupRoles)
        {
            if (_fingerPrint != null) {
                return _fingerPrint.SupportGroups;
            }

            return base.GetSupportedGroups(namedGroupRoles);
        }
        
        public override TlsAuthentication GetAuthentication()
        {
            return _tlsAuthentication;
        }

        public override int[] GetCipherSuites()
        {
            if (_cipherSuites != null)
            {
                return _cipherSuites;
            }

            return base.GetCipherSuites();
        }

        protected override IList<ProtocolName> GetProtocolNames()
        {
            var result = new List<ProtocolName>();

            if (!_applicationProtocols.Any()) {
                return base.GetProtocolNames();
            }

            foreach (var applicationProtocol in _applicationProtocols) {
                if (applicationProtocol.Protocol.Equals(SslApplicationProtocol.Http11.Protocol)) {
                    result.Add(ProtocolName.Http_1_1);
                }

                if (applicationProtocol.Protocol.Equals(SslApplicationProtocol.Http2.Protocol)) {
                    result.Add(ProtocolName.Http_2_Tls);
                }
            }

            return result;
        }

        protected override ProtocolVersion[] GetSupportedVersions()
        {
            // map ProtocolVersion with SslProcols 

            if (_fingerPrint != null) 
            {

                var version = ProtocolVersionHelper.GetFromRawValue(_fingerPrint.ProtocolVersion);

                if (version.IsEarlierVersionOf(ProtocolVersion.TLSv12))
                    return version.Only();

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
        private static readonly ProtocolVersion[] _supportedVersions = new ProtocolVersion[]
        {
            ProtocolVersion.TLSv10,
            ProtocolVersion.TLSv11,
            ProtocolVersion.TLSv12,
            ProtocolVersion.TLSv13
        };

        public static ProtocolVersion GetFromRawValue(int protocolVersion)
        {
            var result =  _supportedVersions.FirstOrDefault(v => (int)v.FullVersion == protocolVersion);

            if (result == null)
            {
                throw new ArgumentException($"Invalid protocol version {protocolVersion}");
            }

            return result;
        }
    }
}
