// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

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
        private readonly SslApplicationProtocol[] _applicationProtocols;
        private readonly FluxzyCrypto _crypto;
        private readonly SslProtocols _sslProtocols;
        private readonly string _targetHost;
        private readonly TlsAuthentication _tlsAuthentication;

        public FluxzyTlsClient(
            string targetHost, SslProtocols sslProtocols,
            SslApplicationProtocol[] applicationProtocols, TlsAuthentication tlsAuthentication,
            FluxzyCrypto crypto)
            : base(crypto)
        {
            _targetHost = targetHost;
            _sslProtocols = sslProtocols;
            _applicationProtocols = applicationProtocols;
            _tlsAuthentication = tlsAuthentication;
            _crypto = crypto;
        }

        public override void Init(TlsClientContext context)
        {
            base.Init(context);
            _crypto.UpdateContext(context);
        }

        public override IDictionary<int, byte[]> GetClientExtensions()
        {
            var extensions = base.GetClientExtensions();

            extensions.Add(0, ServerNameUtilities.CreateFromHost(_targetHost));

            return extensions;
        }

        public override TlsAuthentication GetAuthentication()
        {
            return _tlsAuthentication;
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
}
