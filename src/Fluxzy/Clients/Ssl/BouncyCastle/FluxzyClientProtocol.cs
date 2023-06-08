// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading.Tasks;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;

#pragma warning disable SYSLIB0039

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    public class FluxzyClientProtocol : TlsClientProtocol
    {
        private readonly NssLogWriter _logWriter;
        private TlsSecret? _localSecret;

        public FluxzyClientProtocol(Stream stream, NssLogWriter logWriter)
            : base(stream)
        {
            _logWriter = logWriter;
        }

        public ProtocolName? ApplicationProtocol => PlainSecurityParameters.ApplicationProtocol;

        public ProtocolVersion ProtocolVersion => SessionParameters.NegotiatedVersion;

        public SessionParameters SessionParameters => Context.Session.ExportSessionParameters();

        public SecurityParameters PlainSecurityParameters => Context.SecurityParameters;

        public SslApplicationProtocol GetApplicationProtocol()
        {
            if (ApplicationProtocol == null)
                return SslApplicationProtocol.Http11;

            var str = ApplicationProtocol.GetUtf8Decoding();

            if (str == "http/1.1")
                return SslApplicationProtocol.Http11;

            if (str.Equals("h2"))
                return SslApplicationProtocol.Http2;

            return SslApplicationProtocol.Http11;
        }

        public SslProtocols GetSChannelProtocol()
        {
            var version = ProtocolVersion;

            if (version.IsEqualOrEarlierVersionOf(ProtocolVersion.SSLv3))
#pragma warning disable CS0618
                return SslProtocols.Ssl3;
#pragma warning restore CS0618

            if (version.IsEqualOrEarlierVersionOf(ProtocolVersion.TLSv10))
                return SslProtocols.Tls;

            if (version.IsEqualOrEarlierVersionOf(ProtocolVersion.TLSv11))
                return SslProtocols.Tls11;

            if (version.IsEqualOrEarlierVersionOf(ProtocolVersion.TLSv12))
                return SslProtocols.Tls12;

#if NETCOREAPP
            if (version.IsEqualOrEarlierVersionOf(ProtocolVersion.TLSv13))
                return SslProtocols.Tls13;
#endif

            throw new ArgumentOutOfRangeException("Unknown TLS protocol");
        }

        protected override void CompleteHandshake()
        {
            base.CompleteHandshake();

            // Write key for TLS 1.2 and lower 

            if (ProtocolVersion.IsEqualOrEarlierVersionOf(ProtocolVersion.TLSv12) &&
                Context.Crypto is FluxzyCrypto crypto && crypto.MasterSecret != null) {
                _logWriter.Write(NssLogWriter.CLIENT_RANDOM, PlainSecurityParameters.ClientRandom,
                    crypto.MasterSecret);
            }
        }

        protected override void Handle13HandshakeMessage(short type, HandshakeMessageInput buf)
        {
            base.Handle13HandshakeMessage(type, buf);

            // Here we shall extract the application keys    

            var alreadyUsed = PlainSecurityParameters.TrafficSecretClient == _localSecret;

            if (!alreadyUsed) {
                _logWriter.Write(NssLogWriter.CLIENT_TRAFFIC_SECRET_0, PlainSecurityParameters.ClientRandom,
                    PlainSecurityParameters.TrafficSecretClient.ExtractKeySilently());

                _logWriter.Write(NssLogWriter.SERVER_TRAFFIC_SECRET_0, PlainSecurityParameters.ClientRandom,
                    PlainSecurityParameters.TrafficSecretServer.ExtractKeySilently());

                if (PlainSecurityParameters.ExporterMasterSecret != null) {
                    _logWriter.Write(NssLogWriter.EXPORTER_SECRET, PlainSecurityParameters.ClientRandom,
                        PlainSecurityParameters.ExporterMasterSecret.ExtractKeySilently());
                }
            }
        }

        protected override async Task Handle13HandshakeMessageAsync(short type, HandshakeMessageInput buf)
        {
            await base.Handle13HandshakeMessageAsync(type, buf);

            // Here we shall extract the application keys    

            var alreadyUsed = PlainSecurityParameters.TrafficSecretClient == _localSecret;

            if (!alreadyUsed) {
                _logWriter.Write(NssLogWriter.CLIENT_TRAFFIC_SECRET_0, PlainSecurityParameters.ClientRandom,
                    PlainSecurityParameters.TrafficSecretClient.ExtractKeySilently());

                _logWriter.Write(NssLogWriter.SERVER_TRAFFIC_SECRET_0, PlainSecurityParameters.ClientRandom,
                    PlainSecurityParameters.TrafficSecretServer.ExtractKeySilently());

                if (PlainSecurityParameters.ExporterMasterSecret != null) {
                    _logWriter.Write(NssLogWriter.EXPORTER_SECRET, PlainSecurityParameters.ClientRandom,
                        PlainSecurityParameters.ExporterMasterSecret.ExtractKeySilently());
                }
            }
        }

        protected override void Process13ServerHelloCoda(ServerHello serverHello, bool afterHelloRetryRequest)
        {
            base.Process13ServerHelloCoda(serverHello, afterHelloRetryRequest);

            // Here we shall extract the handshake keys    

            _localSecret = PlainSecurityParameters.TrafficSecretClient;

            _logWriter.Write(NssLogWriter.CLIENT_HANDSHAKE_TRAFFIC_SECRET, PlainSecurityParameters.ClientRandom,
                PlainSecurityParameters.TrafficSecretClient.ExtractKeySilently());

            _logWriter.Write(NssLogWriter.SERVER_HANDSHAKE_TRAFFIC_SECRET, PlainSecurityParameters.ClientRandom,
                PlainSecurityParameters.TrafficSecretServer.ExtractKeySilently());
        }
    }
}
